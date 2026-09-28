# Dados de demonstração — agosto de 2026

O arquivo `seed_demo_august_2026.sql` complementa ou substitui o mock original sem apagar o banco. Ele pode ser executado novamente: os registros de demonstração têm identificadores determinísticos e usam `ON CONFLICT`.

## Execução

Com todas as migrations aplicadas:

```powershell
psql -X -v ON_ERROR_STOP=1 -d dottindb -f backend/tools/seed_demo_august_2026.sql
```

Se o PostgreSQL exigir conexão explícita, use os dados de `appsettings.Development.json` ou as variáveis do `docker-compose.yml`.

## Período e volume esperado

- Período principal: 01/08/2026 a 31/08/2026.
- 5 filiais em três fusos horários.
- 17 usuários ativos e 1 usuário inativo.
- 345 jornadas e 1.380 batidas.
- 5 jornadas aos sábados.
- 5 jornadas em feriados de demonstração.
- 5 faltas intencionais.
- Origens Mobile, Web e Kiosk.
- Evidências de geolocalização para Mobile e Web.
- Atrasos, horas extras, saída antecipada e intervalos estendidos.
- Correções Approved, Pending e Rejected.
- Planos locais de demonstração para liberar as telas operacionais sem cobrança Stripe.

Na tela **Registros**, selecione de `01/08/2026` até `31/08/2026`.

## Acesso

Senha e PIN de todas as contas: `123456`.

| Perfil | Nome | CPF | Empresa |
|---|---|---:|---|
| Owner | Roberto Almeida | 100.200.300-88 | DOTTIN-HQ-001 |
| Administrator | Pedro Lima | 222.333.444-05 | DOTTIN-HQ-001 |
| Manager | Maria Oliveira | 555.666.777-20 | DOTTIN-HQ-001 |
| Employee | Joao Silva | 111.222.333-96 | DOTTIN-HQ-001 |
| Owner | Fernanda Costa | 333.444.555-08 | DOTTIN-RJ-002 |
| Manager | Lucas Mendes | 444.555.666-19 | DOTTIN-RJ-002 |
| Employee | Carlos Santos | 999.888.777-14 | DOTTIN-RJ-002 |
| Owner | Test User | 123.456.789-09 | GOOGLEPLEX-001 |
| Administrator | Sarah Connor | 777.888.999-41 | GOOGLEPLEX-001 |
| Employee | James Kirk | 888.999.000-78 | GOOGLEPLEX-001 |
| Manager | Ellen Ripley | 101.112.131-00 | GOOGLEPLEX-SF-002 |
| Employee | Tony Stark | 202.122.232-24 | GOOGLEPLEX-SF-002 |
| Employee | Peter Parker | 404.142.434-80 | GOOGLEPLEX-NY-003 |

Os CPFs do mock anterior foram corrigidos para dígitos verificadores válidos. Contas adicionais permanecem disponíveis no próprio SQL.

## Cenários rápidos de validação

1. Entre como Owner ou Administrator e confira o histórico de toda a filial.
2. Entre como Manager ou Employee e confira que a tela mostra apenas os registros da própria conta.
3. Confira paginação, troca de período e cálculo de horas.
4. Confira os registros de feriado nas datas 10, 14, 17, 21 e 24 de agosto, conforme a filial.
5. Abra as correções: existe uma aprovada, uma pendente e uma rejeitada.
6. Compare registros Mobile/Web, com localização, e Kiosk, sem localização individual.
7. Confira atrasos nos dias divisíveis por 9, horas extras nos divisíveis por 7 e saídas antecipadas nos divisíveis por 11.

> O script é exclusivamente para desenvolvimento e QA. Ele contém credenciais conhecidas e não deve ser executado em produção.
