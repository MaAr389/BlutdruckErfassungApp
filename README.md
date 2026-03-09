# Blutdruck One Pager (Blazor + Azure Document Intelligence)

One-Pager Blazor-Anwendung für:
- Foto-Upload vom Messgerät (iPhone-kompatibel)
- OCR über Azure Document Intelligence (`prebuilt-read`)
- Speicherung von Systole/Diastole/Puls inkl. Datum/Uhrzeit in Azure SQL
- Sicheren Login per Cookie-Auth
- Visualisierung: Systole/Diastole farblich, Puls als Punkte-Linie (lokales Canvas, kein CDN)
- Docker-ready für Betrieb hinter Traefik (App selbst läuft auf HTTP)

## Kosten
Für OCR kann der kostenlose Tarif `F0` von Azure Document Intelligence verwendet werden (Limitierungen beachten).

## 1) Voraussetzungen
- .NET SDK 8.0
- Azure Document Intelligence Ressource (F0 oder höher)
- Azure SQL Database
- Azure Key Vault

## 2) Secrets in Azure Key Vault
Empfohlene Secret-Namen:
- `AzureDocumentIntelligence--Endpoint`
- `AzureDocumentIntelligence--ApiKey`
- `ConnectionStrings--DefaultConnection`
- `BootstrapAdmin--Username`
- `BootstrapAdmin--Password`

In `appsettings.json` nur `KeyVault:Uri` setzen (keine Klartext-Secrets).

## 3) Azure SQL Connection String (Beispiel)
```text
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<db>;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## 4) Lokal starten
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\\mssqllocaldb;Database=BlutdruckErfassungApp_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
$env:BootstrapAdmin__Username = "admin"
$env:BootstrapAdmin__Password = "SehrSicheresPasswort!123"
$env:AzureDocumentIntelligence__Endpoint = "https://<resource>.cognitiveservices.azure.com/"
$env:AzureDocumentIntelligence__ApiKey = "<api-key>"
$env:Security__ForceSecureCookies = "false"

dotnet run
```

Hinweis: Für Produktion hinter Traefik sollte `Security__ForceSecureCookies=true` bleiben (Default in Production).

## 5) Docker
```bash
docker compose build
docker compose up -d
```

Die App ist dann unter `http://localhost:8080` erreichbar.

## 6) Sicherheitshinweise für DMZ
- TLS-Termination über Traefik (extern 443, intern HTTP zur App)
- Forwarded Headers (`X-Forwarded-For`, `X-Forwarded-Proto`) sind aktiviert
- Auth-Cookie ist `HttpOnly`, `SameSite=Lax`, in Produktion standardmäßig `Secure`
- DataProtection-Keys persistent halten (`/app/keyring` Volume)
- Bootstrap-Admin nur initial verwenden und dann Passwort rotieren
- Alle Secrets (DB, OCR, Bootstrap) in Azure Key Vault halten

## 7) OCR-Verarbeitung
1. Bild wird hochgeladen.
2. Azure Document Intelligence extrahiert Text.
3. Parser erkennt SYS/DIA/PUL und Datum/Uhrzeit.
4. Werte können vor Speicherung manuell korrigiert werden.
