# Mobile / desktop feature parity

## Implemented entry points

- `/register`: real owner registration with desktop password minimum, secure sign-in, retry handling.
- `/onboarding/company`: initial company setup; unfinished onboarding resumes after sign-in/restart.
- `/management/branches/new`: additional branch creation using the same backend.
- `/onboarding/success`: company code and employee web-access link copying. The owner supplies the web Admin URL once; it is stored per owner. The existing mobile configuration does not expose the deployed web Admin URL, so no production hostname is guessed.
- `/management`: owner navigation and branch selection.
- `/management/employees`: searchable, sortable employee directory with active/inactive filters and biometric status.
- `/dashboard` and `/management`: daily management counters, sortable attendance, record details, refresh.
- `/management/dominio`: employee CPF/internal-code mapping.
- `/history` and `/management/reports`: configured Domínio export; monthly/custom-period CSV file sharing.
- `/management/reports`: custom date ranges, night-shift summary, sorting, dedicated holiday-work filter.
- `/management/calendars`: individual calendars, creation, and add/remove operations against the selected calendar.
- `/management/billing`: server-loaded plans, limits, subscription status, renewal date, external checkout/portal, refresh on app resume.

## Design and compatibility

- Existing backend endpoints remain the source of authorization, data, calculations, payroll output, and billing status. No payroll calculation is implemented in the mobile client.
- Domínio defaults match the desktop: normal `1`, night `150`, holiday `250`, process `11`. Company and employee codes must be supplied from the customer's Domínio installation. Mapping does not create employees or execute payment.
- Backend company creation requires CNPJ and a non-zero location, despite the legacy desktop form offering CPF and sending zero coordinates. Mobile follows the backend rules and supports GPS capture/manual coordinates, desktop schedule defaults (08:00–18:00), tolerance 10 minutes, radius 100 meters, and configurable server time zone.
- Password login starts with CPF/password. PIN and existing biometric flows retain company context.
- Route guards restrict owner screens; API authorization remains authoritative. Branch changes remount branch-scoped screens and refresh the stored company code used by kiosk/PIN flows.
- Secure session restoration refreshes tokens on app start. The password-login “remember me” option controls session restoration. Expired/revoked refresh tokens return to sign-in.
- Date query parameters use `yyyy-MM-dd`, independent of device culture. HTTP export errors and empty bodies are rejected before saving/sharing.
- Calendar view no longer deletes against the first calendar. Calendar management selects the exact calendar; overlapping dates do not crash the year view.
- Billing browser return is not treated as proof of payment. Billing uses existing server-configured web return URLs; users return to the app and it reloads server state. No custom app deep-link infrastructure was introduced.
- O registro de ponto exige conexão ativa para preservar horário e evidência GPS válidos; não existe fila offline ou sincronização posterior.

## Profile links

Configure the public destinations at build time with `-p:DottInTermsUrl=https://...`, `-p:DottInPrivacyUrl=https://...`, and `-p:DottInSupportUrl=https://...` (or `mailto:suporte@...` for support). These are public URLs, not secrets. Only HTTPS legal pages are accepted; an invalid configured destination stops app startup. An omitted destination is shown as unavailable rather than acting like a working link. The notifications row is labelled as in preparation because the app has no notification delivery service yet.

## Automated verification

```powershell
dotnet test backend/tests/DottIn.Mobile.Tests/DottIn.Mobile.Tests.csproj
dotnet build backend/clients/DottIn.Mobile/DottIn.Mobile.csproj -f net10.0-android
```

The platform-independent test project links the actual mobile contracts/services and verifies route restrictions, secure session restoration, branch company context, date formatting, mapping data, export parameters, and error handling. It does not replace testing the MAUI UI or a real Domínio import.

## Device and external-service acceptance checklist (not yet executed)

- [ ] Android and iOS: new owner → company → completion; restart before company creation and resume.
- [ ] Existing owner: create another branch, switch branches, verify dashboard/history/profile/calendar data and kiosk company context.
- [ ] Existing employee: password/PIN/biometric login, clock-in/out and breaks with GPS permissions granted/denied.
- [ ] Shared kiosk: reject valid credentials from another company; verify the API also forbids cross-tenant clock actions.
- [ ] Owner directory: CPF/name search, inactive filter, sorting and biometric setup via Profile.
- [ ] Save employee mappings; exercise duplicate/invalid/unmapped errors; compare mobile and desktop TXT bytes for identical inputs, then import into a test Domínio company with existing employees.
- [ ] Custom cross-month period and CSV: compare records/totals with desktop; failed export must not open the file share sheet.
- [ ] Two holiday calendars with overlapping dates: create/add/remove in each; confirm other calendar is unchanged and worked-holiday report matches desktop.
- [ ] Billing test mode: checkout completion/cancellation, existing subscription portal, back to app, server status refresh. Do not run real charges as part of QA.
- [ ] Bloqueio offline/reconexão, sessão expirada, navegação rápida, layout estreito, modo escuro e compartilhamento/clipboard nativos.
- [ ] Configurar destinos reais de Termos, Privacidade e Suporte e verificar a abertura externa em Android/iOS.

The Android build is verified without warnings, and dependency auditing reports no known vulnerable packages.
