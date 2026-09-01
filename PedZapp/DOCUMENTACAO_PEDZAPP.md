# Documentação do PedZapp

> Documento técnico e operacional do estado atual do projeto, levantado diretamente do código em 01/09/2026. Funcionalidades marcadas como **Parcial** ou **Em desenvolvimento** não devem ser tratadas como concluídas em produção.

## Índice

1. [Visão geral](#1-visão-geral)
2. [Tecnologias e estrutura](#2-tecnologias-e-estrutura)
3. [Banco de dados e migrations](#3-banco-de-dados-e-migrations)
4. [Primeira execução](#4-primeira-execução-do-pedzapp)
5. [Admin Master e empresas](#5-admin-master-e-empresas)
6. [Login, rotas e segurança multiempresa](#6-login-rotas-e-segurança-multiempresa)
7. [Painel e configurações da empresa](#7-painel-e-configurações-da-empresa)
8. [Catálogo administrativo](#8-catálogo-administrativo)
9. [Cardápio público, carrinho e checkout](#9-cardápio-público-carrinho-e-checkout)
10. [Pedidos, notificações e WhatsApp](#10-pedidos-notificações-e-whatsapp)
11. [Mesas, comandas e impressão](#11-mesas-comandas-e-impressão)
12. [Relatórios, imagens e disponibilidade](#12-relatórios-imagens-e-disponibilidade)
13. [Operação, backup e publicação futura](#13-operação-backup-e-publicação-futura)
14. [Comandos, troubleshooting e checklist](#14-comandos-troubleshooting-e-checklist)
15. [Roadmap e diagramas](#15-roadmap-e-diagramas)

---

## 1. Visão geral

O **PedZapp** é um SaaS de cardápio digital e operação de pedidos para estabelecimentos, com painel administrativo por empresa, cardápio público, pedidos delivery/retirada, atendimento presencial por mesas e comandas, impressão via navegador e relatórios financeiros.

O problema central que resolve é reunir o catálogo, recebimento de pedidos e operação diária de cada estabelecimento em uma URL própria. Ele é **multiempresa**: cada `Empresa` é um tenant; o `EmpresaId` é a fronteira real dos dados e o `Slug` é sua identificação pública na URL.

```text
ADMIN MASTER
    ↓ administra
EMPRESAS (tenants)
    ↓ cada uma possui
USUÁRIOS + PAINEL + CATÁLOGO + PEDIDOS + MESAS + RELATÓRIOS
    ↓ expõe
/{slug}  → cardápio público
```

Papéis atuais:

- **Administrador Master:** usuário com `IsAdminMaster = true`; administra empresas no backoffice global.
- **Usuário da empresa:** usuário Identity com `EmpresaId`; só pode operar recursos da empresa vinculada.
- **Cliente:** não precisa autenticar para consultar  kout público.

URLs principais:

| Área | URL |
|---|---|
| Cardápio público | `/{slug}` |
| Checkout | `/{slug}/checkout` |
| Confirmação pública | `/{slug}/pedido/{codigo}/confirmacao` |
| Painel da empresa | `/{slug}/painel` |
| Pedidos | `/{slug}/pedidos` |
| Mesas | `/{slug}/mesas` |
| Admin Master | `/AdminMaster` |
| Empresas | `/EmpresasAdmin` |

## 2. Tecnologias e estrutura

### Tecnologias reais

- **.NET 8** (`net8.0`) e ASP.NET Core MVC.
- Razor Views e Razor Pages do ASP.NET Identity.
- Entity Framework Core **8.0.25** com SQL Server; há referência a SQLite, mas o contexto configurado utiliza SQL Server.
- ASP.NET Core Identity para usuários e sessão.
- SignalR para aviso em tempo real de novo pedido.
- JavaScript puro, CSS próprio e Bootstrap distribuído localmente.
- `IHttpClientFactory` para a estrutura da Meta WhatsApp Cloud API.
- Cultura `pt-BR` centralizada, inclusive model binder próprio para decimais.

### Estrutura de pastas

| Pasta | Responsabilidade |
|---|---|
| `Controllers` | Rotas MVC, autorização e composição de Views/JSON. |
| `Models` | Entidades persistidas no banco. |
| `ViewModels` | Modelos específicos das telas e endpoints; evita expor entidades inteiras. |
| `Services` | Regras de negócio, consultas por tenant, impressão, status, SignalR e WhatsApp. |
| `Data` | `ApplicationDbContext`, migrations e seed. |
| `Data/Migrations` | Histórico EF Core e snapshot do modelo. Não editar manualmente. |
| `Areas/Identity` | Páginas customizadas do Identity: login, logout, acesso negado e registro. |
| `Helpers` | Filtro Admin Master, constraint de slug e binder decimal pt-BR. |
| `Hubs` | Hub e grupos privados de pedidos SignalR. |
| `Enums` | Estados de pedido, comanda, mesa, impressão, atendimento e fluxo. |
| `Views` | HTML Razor das áreas pública e administrativa. |
| `wwwroot` | CSS, JavaScript e bibliotecas estáticas. Imagens de empresa/produto não ficam aqui. |

## 3. Banco de dados e migrations

### Contexto e conexão

`Data/ApplicationDbContext.cs` herda de `IdentityDbContext<ApplicationUser>` e reúne Identity e domínio PedZapp. A configuração atual usa `UseSqlServer`.

O `appsettings.json` versionado aponta para SQL Server Express local (`localhost\SQLEXPRESS`) com autenticação integrada e banco `PedZapp`. Não existe senha nesse arquivo. Para produção, a connection string deve ficar em variável de ambiente ou provedor seguro.

Principais conjuntos do contexto:

- `Empresas`, `Categorias`, `Produtos`, `ProdutoImagens`;
- `Adicionais`, `AdicionalCategorias`;
- `BairrosEntrega`, `FormasPagamento`, `HorariosFuncionamento`, `ConfiguracoesLoja`;
- `Pedidos`, `PedidoItens`, `PedidoItemAdicionais`, `ImpressaoPedidos`;
- `Mesas`, `Comandas`, `ComandaItens`, `ComandaItemAdicionais`;
- tabelas `AspNet*` do Identity.

### Relações importantes

```text
Empresa 1 ── N Categoria 1 ── N Produto 1 ── 0..1 ProdutoImagem
Empresa 1 ── N Adicional N ── N Categoria (AdicionalCategoria)
Empresa 1 ── N BairroEntrega / FormaPagamento / HorarioFuncionamento
Empresa 1 ── 1 ConfiguracaoLoja
Empresa 1 ── N Pedido 1 ── N PedidoItem 1 ── N PedidoItemAdicional
Empresa 1 ── N Mesa 1 ── N Comanda 1 ── N ComandaItem
```

Pedidos preservam snapshots de nome e preço dos itens/adicionais. Alterar um produto depois não altera o histórico de vendas.

Índices relevantes incluem slug único de empresa, número do pedido por empresa, chave de idempotência por empresa, código público único de pedido, token de impressão único e uma única comanda ativa por mesa.

### Migrations

A migration mais recente é `20260810180227_AdicionarNotificacaoWhatsAppPedido`. Ela acrescenta opt-in e estado de confirmação WhatsApp ao pedido. Há também `20260810173712_AddDisponibilidadeProduto`.

Para aplicar migrations:

```powershell
# Package Manager Console do Visual Studio
Update-Database

# Terminal na raiz que contém a pasta PedZapp
dotnet ef database update --project .\PedZapp\PedZapp\PedZapp.csproj --startup-project .\PedZapp\PedZapp\PedZapp.csproj
```

> Antes de aplicar em um banco com dados relevantes, faça backup. Não use `EnsureDeleted`, recriação automática ou migrations removidas em produção.

## 4. Primeira execução do PedZapp

1. Instale o SDK .NET 8 e SQL Server Express/SQL Server acessível pela máquina.
2. Abra a solução/pasta e restaure dependências:

   ```powershell
   dotnet restore .\PedZapp\PedZapp\PedZapp.csproj
   ```

3. Confirme `ConnectionStrings:DefaultConnection` em `PedZapp/PedZapp/appsettings.json`. Para outro servidor, prefira User Secrets ou variável de ambiente em vez de versionar uma string sensível.
4. Crie/aplique o banco com `Update-Database` ou `dotnet ef database update`.
5. Inicie:

   ```powershell
   dotnet run --project .\PedZapp\PedZapp\PedZapp.csproj
   ```

6. O `Program.cs` abre um escopo e executa `DbInitializer.SeedAdminAsync` ao iniciar. Esse seed cria o usuário Admin Master quando ele ainda não existe.
7. Entre em `/Identity/Account/Login`; a página de login decide o destino pela identidade. O Admin Master segue para `/AdminMaster`; o usuário de empresa respeita o `returnUrl` (por exemplo, `/{slug}/painel`).

## 5. Admin Master e empresas

### Como criar o primeiro Administrador Master

O mecanismo atual é `Data/Seed/DbInitializer.cs`, chamado em `Program.cs` a cada inicialização. Ele procura o e-mail padrão de administrador e, se não o localizar, chama `UserManager.CreateAsync` com `IsAdminMaster = true` e e-mail confirmado.

**Atenção de segurança:** o seed atual contém e-mail e senha de desenvolvimento codificados. Esta documentação não reproduz a senha. Antes de qualquer uso compartilhado/produção, substitua o seed por configuração segura (User Secrets/variáveis de ambiente) e troque a credencial criada. O arquivo também tenta criar um usuário de teste de empresa **sem `EmpresaId`**, portanto ele não substitui o provisionamento normal de uma empresa.

Como confirmar a criação:

1. Rode o sistema com o banco migrado.
2. Confira a tabela `AspNetUsers` ou faça login em `/Identity/Account/Login` usando a credencial de desenvolvimento definida em `DbInitializer.cs`.
3. O usuário precisa ter `IsAdminMaster = true`.
4. Acesse `/AdminMaster`.

### Funcionalidades do Admin Master

`AdminMasterController` mostra quantidade total de empresas e ativas. O indicador `ReceitaMensal` atualmente é fixo em zero; não representa um consolidado financeiro real (**Parcial**).

`EmpresasAdminController` é protegido por `[Authorize]` e `[AdminMasterAuthorize]` e permite:

- listar empresas: `/EmpresasAdmin`;
- criar: `/EmpresasAdmin/Create`;
- detalhes: `/EmpresasAdmin/Details/{id}`;
- editar: `/EmpresasAdmin/Edit/{id}`;
- excluir: `/EmpresasAdmin/Delete/{id}`.

### Como cadastrar uma nova empresa

1. Faça login como Admin Master e abra **Gerenciar Empresas**.
2. Informe os campos do formulário, inclusive e-mail e senha do acesso da empresa.
3. Ao salvar, o sistema gera um slug com `SlugService` a partir do nome fantasia; se ele já existir, acrescenta um sufixo curto aleatório.
4. Em uma transação, grava `Empresa` e cria `ApplicationUser` com `IsAdminMaster = false` e `EmpresaId = empresa.Id`.
5. A empresa inicia ativa e com plano ativo.
6. O usuário entra pelo login padrão e acessa `/{slug}/painel`.

A exclusão administrativa remove primeiro usuários Identity com o `EmpresaId` da empresa e depois a empresa. Avalie dependências e backup antes de usar exclusão com dados operacionais.

## 6. Login, rotas e segurança multiempresa

### Identity e autorização

- `ApplicationUser` adiciona `IsAdminMaster`, `EmpresaId` e navegação `Empresa` ao usuário padrão do Identity.
- `UserClaimsPrincipalFactory` emite a claim `IsAdminMaster` no login.
- `AdminMasterAuthorizeAttribute` exige sessão e claim válida; retorna challenge para anônimo e forbid para usuário não master.
- O cookie usa `/Identity/Account/Login` como `LoginPath` e `/Home/AccessDenied` como `AccessDeniedPath`.
- Logout usa a página padrão customizada do Identity.

### Slug e rotas

`EmpresaSlugRouteConstraint` valida apenas o **formato** (`a-z`, números e hífen) e bloqueia caminhos reservados como `AdminMaster`, `EmpresasAdmin`, `Identity`, `Home` e módulos administrativos. Ela não consulta banco. A existência da empresa é validada no controller/serviço.

Módulos administrativos com slug usam `[Authorize]` e normalmente executam esta sequência:

```text
slug → Empresa no banco → usuário da sessão → usuario.EmpresaId == empresa.Id
```

Resultado esperado:

- slug inexistente: `NotFound()`;
- sessão ausente: `Challenge()`/login;
- usuário de outra empresa: `Forbid()`;
- acesso válido: consultas sempre filtradas por `EmpresaId`.

### Regras de manutenção obrigatórias

1. Nunca aceite `EmpresaId` de formulário, query string ou JavaScript.
2. Resolva empresa pelo slug no servidor e use seu `Id` em todas as consultas/gravações.
3. Para entidade por `id`, filtre por `Id` **e** `EmpresaId`.
4. Use ViewModels para as telas públicas; não exponha entidades ou bytes de imagem.
5. Não adicione rotas genéricas que possam capturar caminhos administrativos reservados.

SignalR aplica a mesma regra: `PedidosHub` aceita conexão autenticada apenas quando o usuário pertence ao slug; então o adiciona ao grupo privado derivado do `EmpresaId`.

## 7. Painel e configurações da empresa

### Painel

URL: `/{slug}/painel`.

O painel mostra dados reais da empresa autorizada: categorias, produtos, bairros ativos, meios de pagamento ativos, mesas ocupadas, pedidos do dia, pedidos em andamento, vendas do dia e ticket médio. Também traz o status operacional calculado e até quatro ações pendentes.

As pendências atuais verificam: cardápio não publicado, ausência de pagamento ativo, bairros de entrega ausentes, horários válidos ausentes, produtos ativos sem imagem, logo ausente e plano próximo/vencido quando a data existe. Os atalhos preservam o slug. O bloco financeiro detalhado foi removido do painel; relatórios permanecem no módulo próprio.

### Configurações da empresa

URL administrativa: `/{slug}/configuracoes`.

`ConfiguracaoEmpresaService` permite editar, sempre para a empresa da sessão:

- identificação, contatos, documento e endereço;
- logo, descrição e cores;
- aceite de pedidos, pedido mínimo e tempo médio de preparo;
- telefone/WhatsApp de atendimento e redes sociais;
- nome/texto do cardápio e opções de exibir logo/descrição;
- atendimento de mesas, impressão automática de cozinha e observações internas;
- fluxo de pedidos completo ou simplificado.

O formulário pode criar `ConfiguracaoLoja` quando não existir. A loja pública usa o estado da empresa, a publicação de cardápio e o aceite de pedidos; não há uma configuração separada de "loja pública aberta" além desses campos e do horário.

## 8. Catálogo administrativo

### Categorias

O CRUD convencional de `CategoriasController` associa toda categoria ao `EmpresaId` do usuário, não ao formulário. A categoria possui nome, ativa/inativa e ordem. Produtos pertencem a uma categoria; adicionais são vinculados a categorias por `AdicionalCategoria`.

> A rota de categorias é convencional e faz parte da área da empresa; mantenha o filtro por `EmpresaId` em edição e exclusão.

### Produtos

URL: `/{slug}/produtos`.

O módulo permite criar, editar, excluir e alterar disponibilidade. Produto possui categoria, nome, descrição, preço, preço promocional, destaque, tempo de preparo, permissão de observação, ordem e imagem. A categoria escolhida é validada como pertencente à empresa.

- **Ativo:** cadastro disponível/visível no cardápio.
- **Disponível:** pode ser vendido naquele momento. Pode ser alterado sem desativar/apagar o produto.

Produtos indisponíveis não entram no catálogo do cardápio ou na inclusão de novas comandas, e o servidor os revalida ao criar pedido. Imagem é opcional e enviada com validação binária.

### Adicionais

URL: `/{slug}/adicionais`.

Adicional possui nome, descrição, preço, ativo e máximo de seleção opcional. É relacionado a uma ou mais categorias; por isso aparece nos produtos daquela categoria no cardápio e na comanda. Na criação do pedido, o servidor valida se o adicional realmente pertence à categoria do produto e respeita o limite máximo quando configurado.

### Entregas, pagamentos e horários

| Módulo | URL | Estado atual |
|---|---|---|
| Bairros/taxas | `/{slug}/entregas` | cadastro, edição, exclusão, ativo, ordem, taxa, mínimo e tempo estimado. |
| Formas de pagamento | `/{slug}/formas-pagamento` | cria, edita e inativa; garante formas padrão e evita duplicar tipo, exceto “Outro”. |
| Horários | `/{slug}/horarios` | garante os dias da semana, permite até dois períodos e informa aberto/fechado. |
| Administração do cardápio | `/{slug}/cardapio` | publica/pausa e tem editor para alterar ordem de categorias e produtos. |

## 9. Cardápio público, carrinho e checkout

### Cardápio público

URL: `/{slug}`. É anônimo e só usa o slug. Busca a empresa pelo slug e apresenta `Indisponivel` se ela não existir, estiver inativa, com cardápio pausado, sem aceitar pedidos ou fechada pelo horário aplicável.

Mostra categorias ativas; dentro delas, produtos ativos. Produtos mantêm a indicação de indisponibilidade. Adicionais aparecem somente quando ativos, da mesma empresa e vinculados à categoria. Consultas públicas usam `AsNoTracking`, projeções e agrupamento para evitar N+1 e não enviar dados internos.

O botão de opções abre a experiência já existente para selecionar adicionais e observação, quando permitida. Produto sem imagem usa a apresentação de fallback da interface.

### Carrinho

O JavaScript do cardápio mantém carrinho em `localStorage`, com chave contendo o slug (`pedzapp-carrinho-{slug}`). Assim, itens de uma empresa não entram no carrinho de outra. Permite quantidades, adicionais, observação e remoção/edição na experiência pública. Totais na tela são apenas referência: o pedido é recalculado no servidor.

### Checkout

URL: `/{slug}/checkout`; não requer login. Há etapas para tipo de atendimento, dados do cliente, pagamento e revisão.

- atende **Entrega** e **Retirada**; atendimento de mesa é administrativo;
- carrega somente bairros e meios de pagamento ativos do tenant;
- apresenta bairro e taxa; taxa grátis é tratada visualmente;
- rua é opcional para zona rural/nome de local; número, complemento e referência são preservados;
- aceita troco somente quando a forma permitir;
- possui token antiforgery e chave de idempotência;
- registra opt-in operacional opcional para atualizações deste pedido por WhatsApp.

`PedidoService` é a fonte de verdade: resolve a empresa pelo slug, confere disponibilidade, consulta produtos/adicionais/bairro/pagamento pelo `EmpresaId`, recalcula promoções, subtotal, taxa, total e troco, e cria pedido/itens/adicionais em transação. Preço/taxa/EmpresaId do navegador não são confiados.

## 10. Pedidos, notificações e WhatsApp

### Tipos e origens

`OrigemPedido`: `Site`, `Manual` e `Mesa`. O pedido público nasce no checkout; o manual é criado em `/{slug}/pedidos/novo`; o de mesa nasce ao enviar itens da comanda à cozinha.

### Status e fluxos

Estados reais: `Novo`, `Confirmado`, `EmPreparo`, `Pronto`, `SaiuParaEntrega`, `Entregue`, `Cancelado`.

```text
FLUXO COMPLETO
Novo (aguardando confirmação)
  ↓
Confirmado → EmPreparo → Pronto
  ↓ entrega                 ↓ retirada/mesa
SaiuParaEntrega             Entregue
  ↓
Entregue
```

```text
FLUXO SIMPLIFICADO
Novo (aguardando confirmação)
  ↓ confirmar
EmPreparo
  ↓ finalizar
Entregue
```

Cancelamento está disponível apenas conforme a transição válida do serviço. O fluxo é definido por empresa em `ConfiguracaoLoja.TipoFluxoPedido`; completo é o padrão para empresas existentes. A tela de pedidos tem busca, filtro de status e filtro por data, e mostra os status permitidos pelo fluxo.

### Notificação em tempo real

SignalR está implementado em `/hubs/pedidos`. Após o commit de um pedido público, `PedidoNotificacaoSignalRService` envia o evento `NovoPedido` somente para o grupo da empresa. A interface possui alerta visual, som e suporte de reconexão. O banco continua sendo a fonte da verdade: falha no SignalR não desfaz uma venda. Há uma rota de teste somente para Development, ainda protegida pela autorização da empresa.

### WhatsApp Cloud API

**Em desenvolvimento/configuração externa necessária.** A estrutura usa exclusivamente a API oficial Cloud API da Meta:

- `WhatsAppOptions`: `Enabled`, `PhoneNumberId`, `AccessToken`, `ApiVersion`, template e idioma;
- `WhatsAppCloudService`: cliente HTTP oficial com `IHttpClientFactory`, timeout de 15 segundos e template configurável;
- `PedidoWhatsAppNotificacaoService`: valida consentimento, `Pedido.EmpresaId`, origem `Site`, telefone e trava de duplicidade;
- tela de detalhes mostra enviado/falhou e permite reenvio apenas após falha.

Quando a primeira confirmação muda `Novo` para `Confirmado` (fluxo completo) ou `EmPreparo` (simplificado), o status é salvo; a impressão é solicitada; só depois há tentativa de WhatsApp. Falha externa nunca desfaz o pedido. O serviço normaliza telefones brasileiros para E.164 e mascara logs. A previsão usa o bairro para entrega, ou tempo médio de preparo para retirada; sem tempo configurado, usa “a confirmar”.

O template sugerido é `pedido_confirmado`, idioma `pt_BR`, com variáveis: cliente, número público do pedido, previsão e nome da empresa. Não há webhook de entrega/leitura/falha da Meta implementado ainda.

Configurar somente em User Secrets (desenvolvimento) ou variáveis de ambiente (produção), por exemplo:

```powershell
dotnet user-secrets set "WhatsApp:Enabled" "true" --project .\PedZapp\PedZapp\PedZapp.csproj
dotnet user-secrets set "WhatsApp:PhoneNumberId" "[ID SEGURO]" --project .\PedZapp\PedZapp\PedZapp.csproj
dotnet user-secrets set "WhatsApp:AccessToken" "[SEGREDO REMOVIDO - configurar de forma segura]" --project .\PedZapp\PedZapp\PedZapp.csproj
dotnet user-secrets set "WhatsApp:TemplatePedidoConfirmado" "pedido_confirmado" --project .\PedZapp\PedZapp\PedZapp.csproj
```

Na Meta, ainda é necessário criar o app Business, adicionar o produto WhatsApp, configurar/verificar o número, gerar token adequado, criar/aprovar o template e configurar as credenciais. Com `Enabled=false`, nenhuma chamada externa é feita e a confirmação continua normal.

## 11. Mesas, comandas e impressão

### Mesas e comandas

URL: `/{slug}/mesas`. O módulo permite criar mesas, ativar/inativar e abrir comanda apenas para mesa livre. A abertura cria uma comanda ativa, guarda responsável e marca a mesa ocupada.

Na comanda é possível adicionar, editar/remover item pendente, selecionar adicionais e observação. O serviço consulta novamente produto/adicionais, preços e disponibilidade no servidor. Ao **Enviar para a cozinha**, cria pedido de origem `Mesa`, seus itens e a impressão de cozinha; o endpoint `fetch` sempre retorna JSON e inclui `printUrl` somente depois do sucesso.

O fechamento calcula subtotal e taxa de serviço no servidor, define pagamento/troco, fecha a comanda, libera mesa e atualiza pedidos vinculados para o estado final adequado. Após sucesso, responde JSON com URL de impressão da conta final; o navegador abre a impressão.

### Impressão

`BrowserPedidoPrintService` mantém fila/registro `ImpressaoPedido` com token público seguro e índice de idempotência por evento. Há via de cozinha, entrega, comprovante e reimpressão no detalhe do pedido. A URL de impressão administrativa exige autenticação, slug autorizado, código público e token; a conta final exige usuário autorizado e token.

A página de impressão é renderizada no navegador e chama a impressão do navegador. Portanto, **a impressão física automática direta não está implementada**: depende da janela/navegador, impressora configurada e ação do navegador. A interface/serviço foi preparada para substituição futura por agente local ou ESC/POS.

## 12. Relatórios, imagens e disponibilidade

### Relatórios e dashboard financeiro

URL: `/{slug}/relatorios`. O relatório é isolado por empresa e considera somente pedidos `Entregue` e não cancelados. Inclui:

- vendas de hoje, semana e mês; pedidos e ticket médio;
- delivery, retirada, mesa, pedidos manuais, taxas de entrega e serviço;
- cancelamentos; distribuição por pagamentos e atendimento;
- séries dos últimos sete dias e mês; horários de pico;
- Top 5 produtos por período;
- resumo semanal/mensal e indicadores de movimento para fechamento.

O fechamento exibido é informativo: conta pedidos/comandas/mesas abertas e pedidos concluídos sem forma de pagamento. Não executa um fechamento contábil automático.

### Imagens no banco

Logo é armazenada em `Empresa` como bytes, MIME, nome, tamanho e data de atualização. Imagem de produto fica em `ProdutoImagem`, com `EmpresaId`, `ProdutoId`, bytes, MIME e metadados. As imagens não são armazenadas em `wwwroot` nem em Base64.

`ImagemEmpresaService` valida extensão, MIME e assinatura binária antes de salvar. Endpoints públicos:

- `/{slug}/imagem/logo`;
- `/{slug}/produto/{produtoId}/imagem`.

Eles filtram pela empresa do slug, retornam somente bytes/tipo de conteúdo e usam cache público de um dia. A logo usa versão baseada na data de atualização para evitar cache antigo. Isso evita exposição de `EmpresaId` e reduz carga da consulta principal do cardápio.

### Disponibilidade da loja e produto

`StatusLojaService` decide se a loja está aberta verificando: empresa ativa, cardápio publicado, aceite de pedidos e, se existem horários ativos, se o horário atual está dentro de uma janela. Painel, cardápio e checkout reutilizam a mesma decisão.

Para produto, **Ativo** controla existência pública no catálogo e **Disponível** controla venda temporária. Produto ativo mas indisponível pode ser exibido sem permitir compra; servidor também bloqueia inclusão em pedido/comanda.

## 13. Operação, backup e publicação futura

### Backup e restauração

Como imagens ficam no SQL Server, o backup do banco também inclui logos e imagens de produto. Faça backup `.bak` pelo SQL Server Management Studio ou estratégia corporativa do servidor antes de migrations e antes de mudanças estruturais. Para migrar notebook → servidor: restaure o `.bak`, configure connection string do ambiente e aplique apenas migrations pendentes após validar backup.

Nunca publique senha/token no repositório. Itens que não devem entrar no Git:

- connection string de produção com credenciais;
- token da Meta, Phone Number ID, App Secret e WABA ID;
- senhas de Admin Master e empresas;
- certificados e chaves privadas.

### Publicação futura

**Planejado, não implementado neste repositório:** uma instalação Windows com IIS, SQL Server Express/SQL Server, aplicação PedZapp e Cloudflare Tunnel pode expor a aplicação. Para isso serão necessários domínio/HTTPS, variáveis de ambiente, banco com backup, política de logs, atualização segura e configuração SignalR/WebSocket no proxy. Não trate essa arquitetura como deploy pronto.

## 14. Comandos, troubleshooting e checklist

### Comandos úteis

```powershell
# Projeto
dotnet build .\PedZapp\PedZapp\PedZapp.csproj
dotnet run --project .\PedZapp\PedZapp\PedZapp.csproj

# Entity Framework - terminal
dotnet ef migrations add NomeDaMigration --project .\PedZapp\PedZapp\PedZapp.csproj --startup-project .\PedZapp\PedZapp\PedZapp.csproj
dotnet ef database update --project .\PedZapp\PedZapp\PedZapp.csproj --startup-project .\PedZapp\PedZapp\PedZapp.csproj

# Entity Framework - Package Manager Console
Add-Migration NomeDaMigration
Update-Database

# Git
git status
git add .
git commit -m "mensagem"
git push
```

### Problemas comuns

| Sintoma | Verificação segura |
|---|---|
| Banco não conecta | Confira serviço SQL Server Express, nome da instância e `DefaultConnection`; não exponha senha em log. |
| Tabela/coluna não existe | Aplique migrations pendentes no banco correto. |
| 404 no slug | Confirme empresa e formato do slug; caminhos reservados não são cardápio. |
| Access Denied | Confirme login, `ApplicationUser.EmpresaId` e se ele corresponde à empresa do slug. |
| Cardápio indisponível | Verifique empresa ativa, publicação, aceite de pedidos e horários. |
| Produto não aparece/não adiciona | Confirme categoria ativa, produto ativo e `Disponivel=true`. |
| Imagem não aparece | Confira se bytes/MIME foram salvos, endpoint com slug correto e cache do navegador. |
| Aviso SignalR não chega | Confira sessão administrativa, conexão a `/hubs/pedidos`, WebSocket/proxy e grupo da empresa. O banco ainda terá o pedido. |
| Impressão não sai | Confirme abertura da janela, permissões de pop-up e impressora/navegador; não há agente físico direto. |
| WhatsApp não envia | Confira opt-in, origem Site, telefone válido, `WhatsApp:Enabled`, credenciais e template aprovado pela Meta. |

### Checklist operacional

- [ ] Login Admin Master e acesso a `/AdminMaster`.
- [ ] Criar empresa, confirmar slug e usuário com `EmpresaId`.
- [ ] Login da empresa e acesso a `/{slug}/painel`.
- [ ] Tentar outro slug com o mesmo usuário e confirmar bloqueio.
- [ ] Criar categoria, produto, adicional, bairro, pagamento e horários.
- [ ] Publicar cardápio e testar `/{slug}` em sessão anônima.
- [ ] Adicionar produto, adicional e observação ao carrinho.
- [ ] Validar checkout de entrega e retirada, taxa, troco e rua opcional.
- [ ] Confirmar criação de pedido, alerta SignalR e impressão de cozinha.
- [ ] Testar fluxos completo e simplificado.
- [ ] Testar pedido manual.
- [ ] Criar mesa, abrir comanda, enviar itens, imprimir e fechar conta.
- [ ] Conferir relatórios apenas com pedidos entregues.
- [ ] Testar imagem de empresas diferentes e confirmar isolamento.
- [ ] Se WhatsApp estiver configurado, testar opt-in, sucesso, falha e reenvio sem clique duplo.

## 15. Roadmap e diagramas

### Funcionalidades existentes

Multiempresa, Admin Master, cadastro de empresas, catálogo, cardápio público, carrinho, checkout, pedidos, SignalR, mesas/comandas, impressão via navegador, relatórios, imagens no banco, disponibilidade e estrutura WhatsApp Cloud API.

### Pontos parciais / possíveis evoluções

- consolidado financeiro real no dashboard Admin Master;
- webhook assinado da Meta para `sent`, `delivered`, `read` e `failed`;
- credenciais WhatsApp por empresa (a estrutura atual é global e preparada para evoluir);
- agente local/ESC-POS para impressão física sem depender do navegador;
- deploy IIS/Cloudflare Tunnel e operação de produção;
- testes automatizados de integração e interface (não há projeto de testes no repositório atual).

### Diagramas textuais

```text
CLIENTE
  ↓
CARDÁPIO /{slug}
  ↓
CARRINHO (localStorage por slug)
  ↓
CHECKOUT
  ↓ validação e recálculo no servidor
PEDIDO
  ↓
CONFIRMAÇÃO ADMINISTRATIVA
  ↓
IMPRESSÃO DE COZINHA + WHATSAPP OPCIONAL
  ↓
PREPARO / ENTREGA / FINALIZAÇÃO
```

```text
ADMIN MASTER
  ↓ cria
EMPRESA + USUÁRIO IDENTITY (EmpresaId)
  ↓ login
/{slug}/painel
  ↓
CATÁLOGO · PEDIDOS · MESAS · RELATÓRIOS
```

---

## Notas de manutenção

- Este documento não contém tokens, senhas ou connection strings de produção.
- Foi identificado um seed de credenciais de desenvolvimento codificadas em `Data/Seed/DbInitializer.cs`; não reproduza esses valores em documentação pública. A melhoria recomendada é migrá-los para configuração segura.
- Há referência a SQL Server Express local e um aviso de `using` duplicado em `Data/Seed/DbInitializer.cs`; o aviso não altera o funcionamento, mas merece limpeza futura.
- Nenhuma funcionalidade é modificada por este documento.
