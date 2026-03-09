# Blutdruck One Pager (Blazor + Local Vault + Azure SQL)

One-Pager Blazor-Anwendung für:
- Foto-Upload vom Messgerät (iPhone-kompatibel)
- OCR über Azure Document Intelligence (`prebuilt-read`)
- Speicherung von Systole/Diastole/Puls inkl. Datum/Uhrzeit in Azure SQL
- Sicheren Login per Cookie-Auth
- Visualisierung: Systole/Diastole farblich, Puls als Punkte-Linie
- Docker-ready auf HTTP hinter Traefik

## Architektur (ohne Azure Key Vault)
- Secrets liegen im **lokalen Vault** (HashiCorp Vault im Docker-Compose)
- Die App lädt Secrets beim Start aus `secret/blutdruck` (KV v2)
- Azure SQL bleibt als Datenbank konfiguriert

## .env verwenden
1. Datei `.env` anpassen (oder aus `.env.example` erzeugen).
2. Wichtigste Felder:
   - `AZURE_SQL_CONNECTION_STRING`
   - `OCR_ENDPOINT`
   - `OCR_API_KEY`
   - `BOOTSTRAP_ADMIN_USERNAME`
   - `BOOTSTRAP_ADMIN_PASSWORD`
   - `VAULT_DEV_ROOT_TOKEN_ID`

`.env` wird durch `.gitignore` und `.dockerignore` ausgeschlossen.

## Azure SQL Konfiguration (vorgefüllt in .env)
- User: `su-martin`
- Connection String mit TLS (`Encrypt=True`) ist bereits als Variable vorgesehen.

## Start mit Docker
```bash
docker compose build
docker compose up -d
```

Erreichbarkeit:
- App: `http://localhost:8080`
- Vault UI/API (lokal): `http://localhost:8200`

## Vault-Secret Keys
Im Vault-Pfad `secret/blutdruck` werden gesetzt:
- `ConnectionStrings--DefaultConnection`
- `AzureDocumentIntelligence--Endpoint`
- `AzureDocumentIntelligence--ApiKey`
- `BootstrapAdmin--Username`
- `BootstrapAdmin--Password`

`--` wird in der App automatisch in `:` umgewandelt.

## Sicherheitshinweise
- Compose nutzt Vault im **Dev-Mode** (für lokale/DMZ-interne Nutzung).
- Für produktive Umgebung Vault mit persistentem Storage und ohne Dev-Mode betreiben.
- Bitte produktiv Token/Passwörter regelmäßig rotieren.

## OCR Ablauf
1. Bild hochladen.
2. OCR über Azure Document Intelligence.
3. Parser erkennt SYS/DIA/PUL + Datum/Zeit.
4. Werte prüfen und speichern.
