# Matriz e filiais — implementação e validação

## Entrega de código — 09/10/2026

- Admin web: seção `/branches`, navegação exclusiva do Owner, lista de matriz/filiais com status, funcionários ativos e configuração de jornada.
- Formulário responsivo com endereço, CNPJ, contato, coordenadas, fuso, jornada, raio e tolerância. Revisão antes da confirmação, bloqueio de duplo clique e mensagens da API.
- Cópia opcional de fuso, jornada, raio e tolerância da matriz. Não copia localização, endereço ou calendário de feriados, que pertence à própria filial.
- Capacidade da assinatura visível. O limite existente inclui a matriz e conta unidades ativas; `-1` representa capacidade ilimitada. Nenhuma cobrança ou nova assinatura é criada para uma filial.
- Troca de contexto pela lista validada novamente no servidor. Preserva tokens e identidade, persiste a seleção e invalida dados da unidade anterior. O cabeçalho identifica o código da unidade selecionada.
- Rotas de filiais e cadastro inicial protegidas no cliente; criação e consulta de gestão protegidas independentemente no servidor.
- Autorização exige perfil Owner persistido e ativo, além da propriedade persistida da matriz ou da assinatura da mesma matriz. Um token com Owner sem esses vínculos não basta. O tenant nunca vem do formulário.
- Cadastro inicial de matriz continua permitido ao proprietário sem unidades. Matriz/filial são determinados dentro da transação, não pelo campo enviado pelo cliente.
- Locks PostgreSQL por proprietário e documento serializam verificação de capacidade e duplicação entre processos. Ativação de unidades também respeita a capacidade para não contornar o limite.
- Registro persistido de `CreatedByEmployeeId`, junto ao `CreatedAt` existente. Identidade do criador é definida pela API, não pelo navegador.
- Migração `20261009145118_TrackBranchCreator` gerada. Não foi aplicada a bancos existentes ou produção; somente bancos descartáveis de teste receberam migrações.
- Ajustada a validação de fuso para usar a mesma resolução do domínio e aceitar identificadores válidos em Windows e Linux.

## Evidências disponíveis

- Admin: **92 testes aprovados**, incluindo seleção de filial, persistência após refresh e proteção de rotas.
- API, filtro Security/Exports: **70 testes aprovados**.
- Domínio: **23 testes aprovados**.
- Serviços mobile: **57 testes aprovados**. A nova tela é do Admin web responsivo, não uma reformulação da interface nativa.
- Primeira versão dos dois testes `BranchCreationFlowTests` executada com API real e PostgreSQL descartável: criação por proprietário, bloqueio de Employee/Manager/Administrator e Owner sem propriedade, assinatura inelegível, limite atingido, duplicação e concorrência aprovados. Verificou vínculo com a empresa, auditoria e ausência de chamadas ao Stripe ao criar filial.
- Após essa execução, os testes foram ampliados para verificar reativação com limite cheio, proprietário da assinatura diferente do proprietário da matriz e onboarding de nova matriz. A suíte ampliada compila, mas sua execução ainda está pendente.

## Impedimento ambiental e pendências de aceite

O disco C: chegou a **0 bytes livres** durante a preparação do Compose local de QA, e o Docker deixou de responder. O build foi interrompido. Os testes sem Docker foram redirecionados para uma pasta temporária dentro do projeto no D: e concluídos. Não foi realizada limpeza de arquivos ou caches do usuário.

Após liberar espaço e recuperar o Docker:

1. Executar a versão final de `BranchCreationFlowTests` e a regressão de `CompanyJoinLinkFlowTests` com PostgreSQL isolado.
2. Exercitar no navegador: proprietário abre Filiais → revisa cadastro → cria filial → seleciona unidade → recarrega mantendo sessão e contexto.
3. Verificar formulário e listagem em 320/390/768/1366 px, temas claro/escuro, teclado, erros e estados de carregamento.
4. Confirmar navegação e acesso direto negados para Employee, Manager e Administrator, além de acesso cruzado entre tenants.
5. Usar apenas instância e dados descartáveis. Não fazer deploy nem alterar dados de produção.

Exemplo de execução dos testes:

```powershell
dotnet test backend/tests/DottIn.Admin.Tests/DottIn.Admin.Tests.csproj
dotnet test backend/tests/DottIn.WebApi.IntegrationTests/DottIn.WebApi.IntegrationTests.csproj --filter "FullyQualifiedName~BranchCreationFlowTests|FullyQualifiedName~CompanyJoinLinkFlowTests|FullyQualifiedName~Security"
```
