# Homologação da folha de pagamento

O módulo usa `Branch.OwnerId` como empresa e cria uma folha por filial e competência. A
constraint única `(BranchId, Year, Month)` impede duplicação mesmo com solicitações
simultâneas. O proprietário ou administrador do tenant cria, recalcula, preenche e fecha.
O contador precisa aceitar um convite específico para cada filial e só vê folhas
finalizadas; o proprietário pode revogar esse vínculo na página Folha.

## Regras adotadas

- `TimeKeeping`, com ajustes aprovados, é a fonte das horas. Somam-se os intervalos
  de trabalho concluídos e converte-se o total mensal em minutos inteiros, descartando
  segundos residuais apenas após a soma. Jornada aberta ou sequência inválida bloqueia
  o fechamento; suas horas não entram no total do rascunho.
- Entram colaboradores ativos criados antes do fim da competência e colaboradores
  inativos que tenham registros no mês. A conta de contador e a de proprietário não
  entram. O cadastro não armazena data de desligamento ou histórico de filial; casos
  retroativos de colaboradores sem marcações precisam de conferência manual.
- Não existe salário-base ou valor/hora no DottIn. `CalculatedAmount` fica vazio.
  `PaymentAmount` é informado pelo proprietário, inclusive zero quando apropriado.
  O recálculo atualiza horas e cadastro no rascunho, preservando valores já informados.
- Fechamento exige valor, nome, código Domínio e jornadas completas para cada linha.
  Depois dele, nome, código, horas e valor ficam congelados na folha. A exportação
  repete esses valores persistidos e registra cada tentativa bem-sucedida.
- O CSV (`NOME;ID_DOMINIO;SALARIO`) tem UTF-8 com BOM, ponto e vírgula e decimal
  brasileiro com duas casas. É um arquivo de valores para o contador; não substitui
  o TXT de rubricas/horas já existente nem executa pagamento ou importação automática.
- Um contador conta como assento da assinatura ao criar sua conta, seguindo a regra
  comercial atual. Acesso adicional a outra filial reutiliza a mesma conta.

## Verificação automatizada

Com Docker disponível, execute a solução de testes. O teste de fluxo
`PayrollApiFlowTests` sobe um PostgreSQL descartável, aplica as migrations e cobre
proprietário, outro tenant, convite para contador existente e novo, fechamento,
alteração bloqueada, exportação repetida, revogação e saída corrigida.

```powershell
dotnet test backend/DottIn.Web.slnx --no-restore
```

## Homologação manual antes de usar valores reais

1. No desktop, criar a competência, conferir horas, corrigir saídas esquecidas e
   mapear cada colaborador ao código existente no Domínio.
2. Salvar valores, recarregar a página e verificar persistência. Tentar fechar com
   valor/mapeamento ausente ou jornada aberta; a operação deve impedir o fechamento.
3. Fechar a folha. Confirmar que os campos deixam de ser editáveis. Convidar um
   contador novo e aceitar, depois convidar a mesma conta para outra filial.
4. Exportar duas vezes e comparar bytes e valores ao aprovado. Revogar o acesso do
   contador e confirmar que ele não abre mais a folha.
5. Conferir o CSV no Excel e no software de destino com uma empresa de teste.
   Validar competência, código Domínio, acentuação, valor e arredondamento com o
   contador antes de qualquer uso operacional.
