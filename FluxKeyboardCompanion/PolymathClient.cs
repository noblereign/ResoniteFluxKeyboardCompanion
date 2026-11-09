using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Elements.Core;

using FrooxEngine;

namespace FluxKeyboardCompanion;
public record WebsocketMessage(
	[property: JsonPropertyName("route")] string Route,
	[property: JsonPropertyName("message")] string Message
);

class PolymathClient {
	private ClientWebSocket? _ws;
	private static readonly HttpClient _http;

	private readonly CancellationTokenSource _cts = new();

	static PolymathClient() {
		var handler = new HttpClientHandler {
			ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true // flux keyboard websocket uses self signed certificates
		};
		_http = new HttpClient(handler);
		_http.DefaultRequestHeaders.Add("User-Agent", "Resonite");
		_http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
	}

	public async Task ConnectAsync() {
		var cancellationToken = _cts.Token;

		while (!cancellationToken.IsCancellationRequested)
		{
			try {
				var (active, port) = GetFluxConfig();
				if (!active) {
					await Task.Delay(5000, cancellationToken);
					continue;
				}

				string? apiKey = FluxKeyboardCompanion.Config!.GetValue(FluxKeyboardCompanion.ApiKey)?.Trim();
				bool needsRegistration = string.IsNullOrEmpty(apiKey);

				if (!needsRegistration) {
					bool isValid = await ValidateKeyAsync(port, apiKey!, cancellationToken);
					if (!isValid) {
						FluxKeyboardCompanion.Warn("[PolymathClient] Saved API key was rejected by Polymath. Requesting a new one...");
						needsRegistration = true;
					}
				}

				if (needsRegistration) {
					FluxKeyboardCompanion.Msg("[PolymathClient] Requesting app registration. Please approve the prompt in the Polymath UI...");

					NotificationMessage.SpawnTextMessage($"Please approve the connection in Flux Polymath!", colorX.Orange, 0.65f, 15f);

					apiKey = await RegisterAppAsync(port, cancellationToken);

					if (!string.IsNullOrEmpty(apiKey)) {
						FluxKeyboardCompanion.Config!.Set(FluxKeyboardCompanion.ApiKey, apiKey);
						FluxKeyboardCompanion.Config!.Save();
						FluxKeyboardCompanion.Msg("[PolymathClient] API Key successfully received and saved!");
					} else {
						FluxKeyboardCompanion.Warn("[PolymathClient] Registration pending or denied. Retrying in 5 seconds...");
						NotificationMessage.SpawnTextMessage($"Flux Polymath connection was denied! Retrying in 5 seconds...", colorX.Red, 0.65f, 5f);
						await Task.Delay(5000, cancellationToken);
						continue;
					}
				}

				using (_ws = new ClientWebSocket()) {
					_ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
					_ws.Options.SetRequestHeader("User-Agent", "Resonite");
					_ws.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");

					Uri uri = new($"wss://localhost:{port}/v1/configChange");
					await _ws.ConnectAsync(uri, cancellationToken);
					FluxKeyboardCompanion.Msg("[PolymathClient] Websocket connected!");

					var buffer = new byte[1024];
					while (_ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
						var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

						if (result.MessageType == WebSocketMessageType.Close) {
							FluxKeyboardCompanion.Warn($"[PolymathClient] Server closed connection. Status: {_ws.CloseStatus}, Reason: {_ws.CloseStatusDescription}");
							NotificationMessage.SpawnTextMessage($"Lost connection to Flux Polymath!", colorX.Red, 0.65f, 10f);
							break;
						}
					}
				}
			} catch (OperationCanceledException) {
				break;
			} catch (Exception ex) {
				FluxKeyboardCompanion.Warn($"[PolymathClient] Connection error: {ex.Message}");
				if (ex.InnerException != null) {
					FluxKeyboardCompanion.Warn($"[PolymathClient] Inner error: {ex.InnerException.Message}");
				}
				NotificationMessage.SpawnTextMessage($"Flux Polymath connection error: {ex.Message}", colorX.Red, 0.65f, 3f);
			}

			if (!cancellationToken.IsCancellationRequested) {
				try {
					await Task.Delay(2000, cancellationToken);
				} catch (OperationCanceledException) {
					break;
				}
			}
		}
	}

	public void Stop() {
		_cts.Cancel();
	}

	private static async Task<bool> ValidateKeyAsync(int port, string apiKey, CancellationToken cancellationToken) {
		try {
			var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:{port}/v1/authentication/check");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

			var response = await _http.SendAsync(request, cancellationToken);
			return response.IsSuccessStatusCode;
		} catch {
			return false;
		}
	}

	private static async Task<string?> RegisterAppAsync(int port, CancellationToken cancellationToken) {
		try {
			var request = new HttpRequestMessage(HttpMethod.Post, $"https://localhost:{port}/v1/authentication/register");
			var response = await _http.SendAsync(request, cancellationToken);

			if (response.IsSuccessStatusCode) {
				string json = await response.Content.ReadAsStringAsync(cancellationToken);
				using (JsonDocument doc = JsonDocument.Parse(json)) {
					if (doc.RootElement.TryGetProperty("message", out JsonElement msg)) {
						return msg.GetString();
					}
				}
			}
		} catch (Exception e) when (e is not OperationCanceledException) {
			FluxKeyboardCompanion.Warn($"[PolymathClient] Registration request failed: {e.Message}");
		}
		return null;
	}

	public void ChangeContext(string context) {
		FluxKeyboardCompanion.Msg($"Context changing to {context}");
		if (_ws == null) {
			FluxKeyboardCompanion.Warn("Cannot change context: Websocket is null (Not connected yet)");
			return;
		}

		if (_ws.State != WebSocketState.Open) {
			FluxKeyboardCompanion.Warn($"Cannot change context: Websocket is {_ws.State} instead of Open!");
			return;
		}

		WebsocketMessage msg = new("changeContext", context);

		string jsonPayload = JsonSerializer.Serialize(msg);

		var bytes = Encoding.UTF8.GetBytes(jsonPayload);

		_ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
	}

	private static (bool active, int port) GetFluxConfig() {
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string betaPath = Path.Combine(localAppData, "Packages", "FluxGroup.FluxPolymath_42m5gadgr7drm", "LocalState", "flux", "config", "keys.json");

		string legacyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "flux", "config", "keys.json");

		string? activePath = File.Exists(betaPath) ? betaPath :
							File.Exists(legacyPath) ? legacyPath : null;

		if (activePath != null) {
			try {
				var json = File.ReadAllText(activePath);
				var doc = JsonDocument.Parse(json);
				return (
					doc.RootElement.GetProperty("apiActive").GetBoolean(),
					doc.RootElement.GetProperty("openPort").GetInt32()
				);
			} catch { }
		}

		return (false, 0);
	}
}
