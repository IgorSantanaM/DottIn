# Plano de implementação e aceite do MVP DottIn

Estado em 25/09/2026. Este plano cobre o Admin web, a API e o aplicativo mobile. Ele não afirma que uma funcionalidade está pronta só porque existe código ou teste unitário: cada fase termina com evidência de aceite. Preservar as alterações já existentes no repositório e não usar dados ou pagamentos reais nos testes.

## Atualização local — 25/09/2026

- Revogação imediata de sessão implementada: JWT e refresh token carregam a versão da sessão do funcionário; a API verifica essa versão e o estado ativo do funcionário em cada requisição autenticada. Logout troca a versão e apaga todos os refresh tokens da conta em uma transação. Isso encerra a sessão em todos os dispositivos da conta. Tokens de acesso emitidos antes desta migration não têm a claim e exigem renovação por refresh; documentar a transição antes de homologar.
- Migration `AddSessionVersion` criada e aplicada apenas ao banco de desenvolvimento local. Banco novo descartável: 15 migrations, reaplicação idempotente e um plano Free. Backup/restauração em banco descartável confirmou 15 migrations e as contagens de sete tabelas; reaplicação sobre a cópia preservou dados e plano Free. Os verificadores foram corrigidos para preservar aspas nos identificadores PostgreSQL no PowerShell.
- Gate local final: 183 testes aprovados (Mobile 51, Domain 23, Admin 52, API 57), Admin Release e Android Debug aprovados. Playwright padrão: 7 aprovados, 3 Stripe opt-in ignorados. O E2E de logout verifica que JWTs anterior e renovado passam a retornar 401 imediatamente.
- Benchmark aquecido e sequencial de desenvolvimento após a validação por requisição: login p50/p95 92,8/96,7 ms; dashboard 7,5/11,3 ms; histórico paginado 8/10,5 ms; CSV 8,8/57,7 ms. Não há resultado sob concorrência ou meta de carga aprovada. Requisições canceladas pelo cliente agora são classificadas como 499 sem falso erro 500 no log; regressão automatizada incluída.
- Troca de senha/PIN agora rotaciona a versão de sessão, invalidando access e refresh tokens anteriores; o mobile exige senha forte coerente com o domínio, limpa a sessão e orienta novo login. E2E novo em banco descartável validou rejeição de senha fraca sem logout, trocas válidas, rejeição de tokens antigos e login com novas credenciais. A busca exata de código de empresa passou a ignorar caixa para aceitar códigos legados em maiúsculas. O gate HTTP em Windows PowerShell 5 foi corrigido e aprovado; Playwright padrão permaneceu verde.
- API e Admin estão em `localhost`; nenhum deploy foi realizado. Continuam pendentes as decisões da Fase 0, a homologação HTTPS/operação, aceite em dispositivo físico e integrações reais, acessibilidade e carga concorrente, e piloto controlado.

## Progresso local de implementação — 23/09/2026

- Gate `verify-mvp.ps1` executado sem pular Android: 174 testes aprovados (Mobile 44, Domain 22, Admin 52, API 56), build Release do Admin e build Debug Android sem avisos/erros no gate. Checagens HTTP locais de Admin/API também aprovadas.
- Playwright padrão local: 7 testes aprovados, incluindo matriz de leitura multiempresa (filial, funcionários, convites, ajustes, calendários, histórico e exportação negados com 403 nos dois sentidos), rotação de refresh token e revogação de refresh tokens no logout. O teste de ponto foi ajustado para rodadas repetidas com jornada já concluída. Três testes Stripe de mutação ficam intencionalmente opt-in e foram ignorados nesta rodada.
- Nova migration `SeedFreeSubscriptionPlan`: banco vazio recebe um plano Free ativo mesmo sem seed de teste. Teste de 14 migrations em banco descartável e reaplicação idempotente aprovados.
- Backup/restauração de `dottindb` em banco descartável: contagens conferidas para funcionários, filiais, planos, assinaturas, pontos, recibos Stripe e histórico de migrations. Upgrade da cópia de 13 para 14 migrations preservou dados e ID do Free; banco de origem não foi usado como alvo do teste.
- Log de requisição lenta passou a registrar template da rota em vez de URL literal; teste de regressão comprova que CPF e ID de filial não aparecem no log. Um benchmark local reproduzível mede login, dashboard, histórico paginado e CSV. Na segunda rodada aquecida e sequencial: login p50/p95 132/142 ms; dashboard 9/17 ms; histórico 10/25 ms; CSV 12/79 ms. A primeira rodada teve um outlier de dashboard de 1,1 s. Esses números não são metas nem aceite sob carga.
- Mobile: renovação preventiva do access token durante uso, serialização de refresh concorrente, retry único apenas para GET/HEAD após 401 e preservação dos tokens ao trocar de filial. JWT padrão reduzido de 120 para 15 minutos; produção rejeita prazo fora de 1–30 minutos. Build Android e 44 testes mobile verdes. **Limite residual na rodada de 23/09:** logout ainda não invalidava JWT; corrigido na atualização de 25/09 acima.
- API e Admin foram iniciados apenas em `localhost` para os E2E. Nenhum deploy foi realizado. A migration também se aplicou ao banco de desenvolvimento local na inicialização da API.
- Ainda **não** há aceite de dispositivo físico, HTTPS/homologação, webhook em URL pública, alertas, metas de carga aprovadas, Domínio em empresa real nem piloto. As Fases 0, 2, 3, 4 e 5 permanecem abertas conforme dependências abaixo.

## Base já validada

- Cadastro web de proprietário, criação da primeira filial, planos e Checkout Stripe em sandbox; pagamento, webhook automático, portal, cancelamento, retorno ao Free e nova contratação foram exercitados localmente.
- Login e persistência de sessão, telas principais do Admin, ponto web com geolocalização, exportações CSV/TXT e autorização por papel possuem testes de navegador; há suítes automatizadas da API, domínio, Admin e contratos mobile.
- A validação Stripe acima não substitui teste do deployment HTTPS, nem aceite do fluxo no aplicativo mobile ou em produção.

## Fase 0 — Fixar o escopo do piloto (P0)

1. Registrar se o primeiro piloto inclui somente web, web + Android ou também iOS; identificar empresas, filiais, quantidade de usuários simultâneos e dispositivos/navegadores suportados.
2. Decidir se importação no Domínio é condição de entrada para o piloto. Se for, manter a Fase 3 como bloqueadora; se não for, retirar a promessa do escopo comercial inicial.
3. Confirmar e comunicar que o ponto exige conexão ativa: não há fila de registro offline. Notificações push permanecem fora do MVP enquanto não houver serviço de entrega.
4. Definir responsáveis pelos domínios, páginas de Termos/Privacidade/Suporte, conta Stripe, ambiente de homologação, dispositivos físicos e empresa de teste no Domínio.

**Aceite:** escopo e exclusões aprovados por produto/operação, com matriz de plataformas e volume esperado do piloto.

## Fase 1 — Gate reproduzível de build e regressão (P0)

1. Consolidar as alterações pendentes em revisão de código; executar `backend/qa/verify-mvp.ps1` na revisão final, sem pular Android se Android fizer parte do piloto.
2. Executar o build Release do Admin e o build instalável Android. Se iOS estiver no escopo, produzir e instalar também um build assinado em infraestrutura compatível.
3. Executar a suíte Playwright padrão contra um banco de homologação previsível. Manter os testes Stripe que criam clientes/assinaturas como etapa opt-in, numa conta sandbox isolada e com limpeza identificável dos dados de QA.
4. Validar migrações em cópia descartável de banco, inicialização em banco novo, idempotência de seed e o caminho de atualização de uma versão anterior. Provisionar os planos Stripe e respectivos Price IDs por processo operacional documentado; o seed de teste não deve ir a produção.
5. Publicar resultados, logs de falha e artefatos de build associados ao mesmo commit.

**Aceite:** quatro suítes .NET verdes, E2E web verde, builds das plataformas escolhidas instaláveis, migração testada e nenhuma falha ou aviso de release sem decisão registrada.

## Fase 2 — Homologação HTTPS e operação (P0)

1. Implantar API, Admin e PostgreSQL num ambiente de homologação próximo ao de produção, com HTTPS, domínio real de teste, CORS restrito, segredos fora do repositório e URLs de retorno Stripe corretas.
2. Configurar e verificar os Price IDs da conta Stripe correspondente, webhook HTTPS assinado, banco, Azure Blob e RabbitMQ quando habilitado. Repetir cadastro → pagamento → ativação → portal → cancelamento → nova contratação nesse ambiente.
3. Persistir as chaves de Data Protection em volume privado e compartilhado entre réplicas; exercitar reinício/deploy sem invalidar sessões e links de convite ainda válidos.
4. Configurar logs estruturados, correlação por requisição, métricas de latência/erros e alertas para API indisponível, banco indisponível, falha de webhook e fila parada. Evitar CPF, token e segredo em logs.
5. Executar backup e **restauração** do PostgreSQL e das chaves necessárias em ambiente separado; documentar RPO/RTO, responsáveis, rollback e procedimento de incidente.
6. Confirmar licenciamento/remoção do aviso MediatR para produção e configurar destinos oficiais de Termos, Privacidade e Suporte.

**Aceite:** `health/live` e `health/ready` saudáveis após deploy/reinício; teste Stripe de homologação completo; restore demonstrado; alertas recebidos; checklist de configuração de produção aprovado sem expor segredos.

## Fase 3 — Aceite funcional em dispositivos e integrações (P0 para plataformas/integrações do escopo)

1. Em aparelhos Android e, se incluído, iOS: proprietário novo → empresa → plano → retorno do Checkout; interromper antes de criar empresa, reiniciar e retomar onboarding.
2. Proprietário existente: criar/trocar filial e conferir que dashboard, histórico, perfil, calendário, cobrança e contexto de quiosque passam a usar a filial correta.
3. Funcionário: login por CPF/senha, PIN e biometria; ponto de entrada/saída e pausas com GPS permitido e negado; validar horário da filial, raio, precisão e evidência armazenada. Repetir com sessão expirada, revogada, app reiniciado, rede intermitente e dispositivo sem conexão.
4. Quiosque compartilhado: negar credenciais e ações de outra empresa no cliente **e** na API. Exercitar papéis Owner, Administrator, Manager e Employee nas telas e endpoints sensíveis.
5. Diretório e calendários: CPF/nome, filtros, ordenação, biometria e dois calendários com datas sobrepostas; conferir que edição/remoção não altera a outra filial ou calendário.
6. Relatórios/exportação: período que cruza mês, turno noturno, feriado trabalhado, erro de download e CSV/TXT no compartilhamento nativo; comparar registros, totais e bytes entre web e mobile.
7. Se Domínio estiver no escopo: preencher códigos reais da empresa de teste, testar CPF/código duplicado, inválido e não mapeado, importar TXT no Domínio e reconciliar resultado com os relatórios DottIn.
8. Testar links oficiais de Termos/Privacidade/Suporte, clipboard e compartilhamento nativos, modo escuro e layout estreito.

**Aceite:** checklist em `backend/clients/DottIn.Mobile/MOBILE_PARITY.md` preenchido com aparelho/versão, passos, evidência e resultado; defeitos bloqueadores corrigidos e retestados. Testes de contrato não substituem este aceite.

## Fase 4 — Segurança, desempenho e UX sob carga (P0 antes de ampliar o piloto)

1. Repetir a matriz multiempresa com duas empresas reais de homologação: acesso a filial, funcionário, histórico, ajuste, exportação, convites, calendário e cobrança; todos os acessos cruzados devem falhar sem vazar dados.
2. Testar renovação/revogação de sessão, logout, links expirados, abuso de login/rate limit e webhooks inválidos, duplicados e fora de ordem. Revisar armazenamento local de sessão e proteção de dados pessoais.
3. Definir metas de latência e volume conforme a Fase 0. Medir p50/p95 de login, dashboard, histórico paginado, registros de ponto, exportações e atualização de assinatura com usuários simultâneos; corrigir consultas e renderizações que excederem as metas.
4. Testar acessibilidade e UX em teclado, foco, contraste, mensagens de erro/loading, Chrome/Edge e tamanhos de tela suportados. Incluir conexões lentas e perda de rede durante ações críticas.

**Aceite:** relatório com cenários, carga, métricas, falhas encontradas e retestes; nenhum vazamento entre tenants ou perda/duplicação de ponto; metas de desempenho acordadas cumpridas no ambiente de homologação.

## Fase 5 — Piloto controlado e decisão de lançamento (P0)

1. Fazer ensaio de implantação e rollback, congelar a versão candidata e executar novamente as Fases 1–4 que possam ser afetadas pelo último commit.
2. Habilitar uma ou duas empresas piloto com suporte designado; acompanhar login, ponto, erros, latência, webhooks e exportações diariamente durante a janela combinada.
3. Registrar incidentes e feedback, corrigir bloqueadores e repetir o aceite afetado. Formalizar decisão **go/no-go** por produto, engenharia e operação.

**Aceite:** evidências anexadas à versão candidata, responsáveis de suporte definidos, plano de reversão ensaiado e decisão de lançamento registrada.

## Dependências externas e ordem

- A Fase 0 precede metas de carga e a decisão sobre iOS/Domínio. Fases 1 e 2 podem avançar em paralelo; a homologação implantada é pré-requisito para os testes integrados da Fase 3 e a carga da Fase 4.
- Dispositivos físicos, empresa de teste no Domínio, infraestrutura de homologação, credenciais/preços Stripe, páginas legais e decisão sobre licença dependem de acesso ou decisão do responsável; não podem ser comprovados apenas pelo repositório.
- Para um piloto **somente web**, o aceite mobile da Fase 3 pode ser adiado formalmente. Para anunciar **web + mobile**, não o adiar. A Fase 2 e os testes de segurança/carga relevantes continuam necessários em ambos os casos.
