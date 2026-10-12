# Configurações gerais — implementação e validação

Atualização: 09/10/2026. Nenhum deploy foi realizado.

## Entregas

- Web: `/settings`, acessível a usuários autenticados, inclusive proprietários ainda sem empresa/assinatura configurada.
- Mobile: `/settings` (também `/profile`) e `/settings/unit`.
- Perfil pessoal: edição do nome, CPF somente leitura e atualização do cabeçalho/sessão local, sem aceitar ID de outra pessoa ou alteração de papel.
- Segurança: senha atual obrigatória para alterar senha/PIN; revogação de sessões e refresh tokens após a alteração; encerramento explícito de todas as sessões. Falhas de comunicação não são apresentadas como revogação bem-sucedida.
- Aparência: claro, escuro e sistema, persistidos por conta e dispositivo/navegador; reação a mudanças no tema do sistema.
- Unidade: endereço, coordenadas, fuso, jornada padrão, raio, tolerância e calendário. Captura de localização somente mediante ação explícita e permissão, sem rastreamento em segundo plano.
- Administração da unidade restrita a Owner/Administrator do tenant autorizado. Employee, Manager e Accountant têm configurações pessoais, não administrativas.
- Atalhos para empresa/filiais, assinatura e integração Domínio nos fluxos existentes. Não adiciona integração nova ou altera cobrança.
- Links de termos, privacidade e suporte não configurados são indicados como indisponíveis, sem links fictícios.
- Salvamento por seção, confirmação, aviso de alterações não salvas, estados de processamento e mensagens de conflito/erro.

## API e consistência

| Endpoint | Operação |
| --- | --- |
| `GET /api/settings/me` | Perfil da identidade autenticada |
| `PUT /api/settings/me` | Nome + versão esperada |
| `PUT /api/settings/password` | Senha atual + nova senha |
| `PUT /api/settings/pin` | Senha atual + novo PIN |
| `GET /api/settings/branches/{branchId}` | Configurações administrativas da unidade autorizada |
| `PUT /api/settings/branches/{branchId}` | Seção + versão esperada + campos da seção |

Alterações usam transação com bloqueio de linha e estratégia de retry do EF/Npgsql. Versões são normalizadas à precisão de timestamp do PostgreSQL. Escritas concorrentes/stale retornam 409, evitando sobrescrita silenciosa. Senha e PIN usam a política de rate limiting de autenticação existente.

Alterações de unidade registram autor, seção, instante UTC e valores anteriores/posteriores em `BranchSettingsAudits`. Credenciais não entram nesse registro. Alterar o fuso não reescreve os instantes históricos em UTC; alterar a jornada padrão não altera automaticamente as jornadas individuais; folhas fechadas não são reabertas.

## Cache e carregamento

`AdminApiClient.GetBranchManagementAsync` foi adicionado com cache de cinco minutos, `forceRefresh` e cancelamento. `CreateManagedBranchAsync` cria a filial e invalida gerenciamento, lista do proprietário e assinatura somente após resposta bem-sucedida.

Perfil/configurações web usam cache de cinco minutos. Seções administrativas e calendários são carregados sob demanda. Salvar a unidade invalida relógio, dashboard, histórico e consultas de feriados dependentes, sem invalidar listas de funcionários ou outras unidades. O cache do dashboard persistido também é limpo para não restaurar regras antigas.

## Banco: migration ainda não aplicada

Migration nova: `20261009181904_AuditBranchSettings`. Cria uma tabela de auditoria e seu índice; não reescreve dados existentes.

Ela precisa ser aplicada no banco usado pela API antes de salvar configurações de unidade. Não foi aplicada em nenhum banco nesta execução. Em ambiente local com migrações automáticas habilitadas, a inicialização aplica o schema; em outros ambientes, seguir o procedimento controlado de migrations do projeto. Isso não autoriza deploy.

## Verificações realizadas

- Suítes executadas com sucesso: Admin 109, API sem Docker 108, Mobile 68 e Domain 27 (312 testes, zero falhas).
- Compilação Web/API e Android concluída; Android sem erros ou avisos.
- Interface web real compilada, servida em fixture local com respostas sintéticas: perfil, salvamento da unidade, confirmação de descarte, jornada, regras, tema após refresh e diferenças entre Owner/Employee.
- Responsividade verificada em 320, 390, 768 e 1366 px, sem overflow horizontal. Seletor compacto no celular e navegação lateral no desktop.
- A fixture visual não usa o banco real, não prova a autorização do backend e não envia dados para produção. Senhas, PIN e permissões de GPS não foram alterados pela navegação de QA.

## Validações ainda pendentes

1. Executar `SettingsApiFlowTests` com PostgreSQL isolado via Testcontainers. O Docker daemon não respondeu neste ambiente; testes foram compilados, mas não executados. Cobrem permissão real, isolamento entre tenants, concorrência, auditoria, versões após persistência e revogação de credenciais.
2. Aplicar a migration e repetir o fluxo integrado web/API em um banco local de teste.
3. Executar em Android físico/emulador: localização concedida/negada, configurações da unidade, credenciais e retomada do tema após reinício do aplicativo.
4. Compilar e testar iOS em um host compatível; não validado no Windows.
5. Configurar os endereços públicos reais de termos, privacidade e suporte quando disponíveis.

Com Docker operacional, executar a partir da raiz do repositório:

```powershell
dotnet test backend/tests/DottIn.WebApi.IntegrationTests/DottIn.WebApi.IntegrationTests.csproj --filter FullyQualifiedName~SettingsApiFlowTests
```

O comando utiliza banco descartável isolado; não apontá-lo para produção.
