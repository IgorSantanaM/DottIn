# Validação do convite compartilhado

O vínculo é confirmado no banco pelo proprietário da empresa (tenant). Um convite
para outra filial da mesma empresa não transfere a conta, não altera seu papel e
não consome outro assento. Convites inválidos/expirados continuam sendo rejeitados.

## Regressões automatizadas

Na raiz do repositório:

```powershell
dotnet test backend/tests/DottIn.Admin.Tests/DottIn.Admin.Tests.csproj
dotnet test backend/tests/DottIn.WebApi.IntegrationTests/DottIn.WebApi.IntegrationTests.csproj
dotnet test backend/tests/DottIn.Domain.Tests/DottIn.Domain.Tests.csproj
```

`CompanyJoinLinkFlowTests` exige Docker e cria um PostgreSQL descartável com
Testcontainers, aplicando as migrations e usando os endpoints HTTP reais de login,
resolução e cadastro. Não utiliza bancos existentes, Stripe ou RabbitMQ. Para rodar
somente os testes que não precisam de Docker, use `--filter "Category!=Docker"`.

Cobertura específica:

- Sessão existente validada antes de resolver o convite; snapshot não prova vínculo.
- Refresh revogado limpa a sessão e permite o fluxo anônimo.
- Falha temporária preserva a sessão e não confirma vínculo nem invalida o convite.
- Login de membro da mesma filial ou outra filial do mesmo proprietário.
- Proprietário sem filial atribuída não é rebaixado ao abrir seu próprio convite.
- Limite de assentos não bloqueia membros existentes; bloqueia novos ingressos.
- Conta de outra empresa, conta inativa e convite inválido/expirado rejeitados.
- Novo ingresso automático e novo cadastro mantêm o papel Funcionário.
- Aviso consumido uma vez e não reaproveitado para outro usuário.
- Mensagem amigável para credenciais inválidas.
- Ex-funcionário com senha/PIN corretos recebe 403 `employee_inactive` e mensagem
  de vínculo inativo, sem cookie nem sessão nova. Credenciais incorretas continuam
  retornando 401, sem revelar o estado da conta.

## Conferência visual local

Use uma instância local descartável com o seed de demonstração. Gere um link com
o proprietário e verifique:

1. Sem sessão: aparecem as opções Já tenho conta e Criar conta; CPF recebe máscara.
2. Já tenho conta: login de um membro mostra o check e redireciona com um aviso.
3. Com sessão válida: abrir o link mostra a mesma confirmação, sem formulário.
4. F5 no dashboard: sessão preservada e aviso não repetido.
5. Criar conta: cadastro entra automaticamente, sem falso aviso de membro existente.
6. Largura de 390 px: confirmação sem transbordamento ou sobreposição de header.

Validação executada em 09/10/2026 com build Release no Docker local. Nenhum deploy
é necessário para executar estes testes.
