# HomeMcp Server — End-to-End Testing Guide

Complete walkthrough for testing the server locally using `curl` and `grpcurl`.

## Prerequisites

```bash
# .NET 10 SDK
dotnet --version   # 10.x

# grpcurl
go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest
# or: brew install grpcurl

# jq (optional, for pretty JSON)
which jq
```

---

## 1. Start the Server

```bash
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project src/HomeMcp.Server/HomeMcp.Server.csproj
```

Expected output:
```
Now listening on: https://127.0.0.1:5201
```

> In Development mode ASP.NET Core automatically applies the dev TLS certificate,
> giving you proper HTTP/2 over HTTPS. Use `-k` with curl and `-insecure` with grpcurl
> to skip cert validation locally.

---

## 2. First-Time Setup

Done once. Persisted to `data/setup.json`.

```bash
# Check whether setup is already complete
curl -sk https://localhost:5201/admin/setup/status | jq

# If complete=false, initialise the server
curl -sk -X POST https://localhost:5201/admin/setup/init \
  -H "Content-Type: application/json" \
  -d '{
    "password": "supersecret123",
    "serverName": "My Home MCP",
    "defaultLocale": "en-US"
  }' | jq
```

Expected response:
```json
{ "ok": true }
```

---

## 3. Pair a Device

### 3a. Admin generates a PIN (10-minute TTL, single-use)

```bash
curl -sk -X POST https://localhost:5201/admin/pairing/pin | jq
```

Response:
```json
{
  "pin": "G66GE4",
  "expiresInSeconds": 600
}
```

### 3b. Device pairs using the PIN

```bash
curl -sk -X POST https://localhost:5201/api/v1/pair \
  -H "Content-Type: application/json" \
  -d '{
    "pin": "G66GE4",
    "displayName": "Living Room Speaker",
    "locale": "en-US",
    "capabilities": {
      "stt": false,
      "tts": true,
      "audioPlayback": true,
      "mediaPlayer": true,
      "display": false,
      "wakeWord": false
    }
  }' | jq
```

Response:
```json
{
  "deviceId": "019e770068867d8c82f572d8520bb157",
  "deviceToken": "JPyw7aqsN8k/KwJ5+71q6r/Xgy3hwAM0/YY4/f1D52o="
}
```

> **Save `deviceId` and `deviceToken`** — the server never stores the plaintext token again.

---

## 4. Check Server Health

```bash
curl -sk https://localhost:5201/admin/health | jq
```

---

## 5. Stream a Conversation with grpcurl

Use `-insecure` to skip dev-cert validation. gRPC runs over HTTP/2 via TLS (ALPN).

### 5a. Using server reflection (no proto files needed)

```bash
# List available services
grpcurl -insecure localhost:5201 list

# Describe the Converse RPC
grpcurl -insecure localhost:5201 describe home_mcp.v1.Assistant
```

### 5b. Get a session ID (send HelloEvent)

```bash
grpcurl -insecure \
  -d '{ "hello": { "device_id": "YOUR_DEVICE_ID", "device_token": "YOUR_DEVICE_TOKEN", "client_version": "1.0.0", "locale": "en-US", "capabilities": { "audio_playback": true, "media_player": true } } }' \
  localhost:5201 home_mcp.v1.Assistant/Converse
```

Response:
```json
{ "sessionStarted": { "sessionId": "019e7700b5...", "userId": "..." } }
```

### 5c. Full conversation — file-based (works in all shells including fish)

Create `payload.json`:
```json
[
  {
    "hello": {
      "device_id":    "YOUR_DEVICE_ID",
      "device_token": "YOUR_DEVICE_TOKEN",
      "client_version": "1.0.0",
      "capabilities": {
        "stt": false, "tts": true,
        "audio_playback": true, "media_player": true,
        "display": false, "wake_word": false
      },
      "locale": "en-US"
    }
  },
  {
    "text": {
      "session_id": "SESSION_ID_FROM_HELLO_RESPONSE",
      "text": "Play 50 Cent In Da Club"
    }
  }
]
```

```bash
grpcurl -insecure -d @ localhost:5201 home_mcp.v1.Assistant/Converse < payload.json
```

or

```bash
grpcurl -insecure -d @ localhost:5201 home_mcp.v1.Assistant/Converse <<EOF
{
  "hello": {
    "device_id": "YOUR_DEVICE_ID",
    "device_token": "YOUR_DEVICE_TOKEN",
    "client_version": "1.0.0",
    "capabilities": {
      "stt": false,
      "tts": true,
      "audio_playback": true,
      "media_player": true,
      "display": false,
      "wake_word": false
    },
    "locale": "en-US"
  }
}
{
  "text": {
    "session_id": "YOUR_SESSION_ID",
    "text": "Play 50 Cent In Da Club"
  }
}
EOF
```

### 5d. Heredoc form (bash/zsh only)

```bash
grpcurl -insecure -d @ localhost:5201 home_mcp.v1.Assistant/Converse <<'EOF'
{ "hello": { "device_id": "YOUR_DEVICE_ID", "device_token": "YOUR_DEVICE_TOKEN", "client_version": "1.0.0", "locale": "en-US" } }
{ "text": { "session_id": "SESSION_ID", "text": "Play 50 Cent In Da Club" } }
EOF
```

---

## 6. Example Queries

All queries go in the `"text"` field of `TextInputEvent`.

### Music playback

```
Play 50 Cent In Da Club
Play any 50 Cent song
Play hip-hop
```

### Music discovery

```
What songs does 50 Cent have?
Show me albums by 50 Cent
What is on the album Get Rich or Die Tryin?
Search for Candy Shop
Find music by artist Eminem
```

### Paginated listing

```
List hip-hop songs
Show me more hip-hop songs
List songs from the album Encore
```

### Lyrics

```
What are the lyrics for In Da Club by 50 Cent?
Show me lyrics for Many Men
```

---

## 7. Expected gRPC Response Flow

```jsonc
// Session confirmed
{ "sessionStarted": { "sessionId": "019e...", "userId": "..." } }

// Jellyfin plugin invoked
{ "toolCallStarted":  { "pluginId": "io.jellyfin", "toolName": "jellyfin.search_tracks" } }
{ "toolCallFinished": { "pluginId": "io.jellyfin", "toolName": "jellyfin.search_tracks",
    "resultJson": "{\"total\":1,\"tracks\":[{\"id\":\"f380...\",\"title\":\"In da Club\",...}]}" } }

// LLM streams the reply
{ "textDelta": { "text": "Playing \"In da Club\" by 50 Cent." } }

// Turn complete
{ "turnDone": { "reason": "TURN_DONE_REASON_COMPLETED" } }
```

---

## 8. Admin REST Endpoints

All `/admin/*` are internal (admin UI / dev tools only).

| Method | URL | Description |
|--------|-----|-------------|
| `GET`  | `/admin/health` | Server health + uptime |
| `GET`  | `/admin/setup/status` | Whether first-time setup is complete |
| `POST` | `/admin/setup/init` | Complete first-time setup |
| `POST` | `/admin/pairing/pin` | Generate one-time device pairing PIN |
| `GET`  | `/admin/sessions/{id}` | Inspect a session by ID |

---

## 9. Pairing API (External)

| Method | URL | Description |
|--------|-----|-------------|
| `POST` | `/api/v1/pair` | Pair a new device using a PIN from the admin |

---

## 10. Full Copy-Paste Script (bash/zsh)

```bash
BASE=https://localhost:5201

# 1. Start server (in another terminal):
#    ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/HomeMcp.Server/HomeMcp.Server.csproj

# 2. Setup (skip if already done)
curl -sk -X POST $BASE/admin/setup/init \
  -H "Content-Type: application/json" \
  -d '{"password":"supersecret123","serverName":"My Home","defaultLocale":"en-US"}' | jq

# 3. Generate pairing PIN
PIN=$(curl -sk -X POST $BASE/admin/pairing/pin | jq -r '.pin')
echo "PIN: $PIN"

# 4. Pair device
PAIR=$(curl -sk -X POST $BASE/api/v1/pair \
  -H "Content-Type: application/json" \
  -d "{\"pin\":\"$PIN\",\"displayName\":\"Test Device\",\"locale\":\"en-US\",\"capabilities\":{\"mediaPlayer\":true,\"audioPlayback\":true}}")
DEVICE_ID=$(echo $PAIR | jq -r '.deviceId')
DEVICE_TOKEN=$(echo $PAIR | jq -r '.deviceToken')
echo "Device ID:   $DEVICE_ID"
echo "Device Token: $DEVICE_TOKEN"

# 5. Get session ID
SESSION_ID=$(grpcurl -insecure -d \
  "{\"hello\":{\"device_id\":\"$DEVICE_ID\",\"device_token\":\"$DEVICE_TOKEN\",\"client_version\":\"1.0.0\",\"locale\":\"en-US\"}}" \
  localhost:5201 home_mcp.v1.Assistant/Converse \
  | jq -r 'select(.sessionStarted) | .sessionStarted.sessionId' | head -1)
echo "Session ID: $SESSION_ID"

# 6. Play a 50 Cent song
grpcurl -insecure -d @ localhost:5201 home_mcp.v1.Assistant/Converse <<EOF
{ "hello": { "device_id": "$DEVICE_ID", "device_token": "$DEVICE_TOKEN", "client_version": "1.0.0", "locale": "en-US" } }
{ "text": { "session_id": "$SESSION_ID", "text": "Play 50 Cent In Da Club" } }
EOF
```
