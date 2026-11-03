# ChamadosTI

**Sistema profissional de gerenciamento de Service Desk e Help Desk para governança de TI**

> 🎓 **Projeto de portfólio:** desenvolvido para demonstrar, de ponta a ponta, arquitetura de aplicações ASP.NET Core, regras de negócio de ITSM/ITIL e cobertura de testes unitários. As decisões técnicas e as regras de negócio estão documentadas nas seções abaixo.

## 💻 Sobre o Projeto
O **ChamadosTI** é uma aplicação corporativa sênior projetada para centralizar o ecossistema de suporte de tecnologia de uma organização. O sistema vai além do registro básico de chamados, oferecendo uma esteira completa de rastreabilidade de ativos afetados e um motor dinâmico e inteligente para o cálculo e cumprimento de metas de acordos de nível de serviço (SLA), em total alinhamento com as boas práticas da **ITIL**.

## 🛠️ Tecnologias e Ferramentas

### Back-End & Infraestrutura
*   **Framework Principal:** [ASP.NET Core 10.0 (MVC)](https://dotnet.microsoft.com/pt-br/apps/aspnet) com arquitetura limpa e desacoplada.
*   **Mapeamento O/RM:** [Entity Framework Core 10.0](https://microsoft.com) para persistência de dados.
*   **Mapeamento Avançado:** **Fluent API** centralizada por classes de configuração para validação rigorosa de metadados e restrição de chaves estrangeiras duplas.
*   **Banco de Dados:** Microsoft SQL Server.
*   **Gerenciamento de Identidade:** **ASP.NET Core Identity** para autenticação e autorização robusta baseada em perfis de acesso (Role-Based Access Control).

### Testes Automatizados
*   **Framework de Testes:** [xUnit](https://xunit.net) 2.9 com projeto dedicado `ChamadosTi.Tests`.
*   **Mocks:** [Moq](https://github.com/moq/moq4) para isolar a `TicketsController` de `IUsuarioService`, `UserManager` e `IStringLocalizer`.
*   **Banco em Memória:** SQLite em memória (`Microsoft.Data.Sqlite`), com um banco isolado por teste, o que permite validar consultas e gravações reais do EF Core sem depender do SQL Server.
*   **Controle de Tempo:** `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) para testar cálculos de SLA e prazos em instantes controlados.

### Front-End & Interface
*   **Estilização & Layout:** [Bootstrap 5](https://getbootstrap.com) para construção de um design responsivo, elegante e limpo.
*   **Ícones Operacionais:** [Bootstrap Icons](https://getbootstrap.com) atuando como âncoras visuais nos menus.
*   **Dinamismo Client-Side:** [jQuery](https://jquery.com) e JavaScript corporativo puro para manipulação de DOM de alta performance.
*   **Validação de Formulários:** `jQuery Validation` e `jQuery Validation Unobtrusive` para checagem imediata de regras de input.
*   **Gerenciador de Bibliotecas:** **Libman (Library Manager)** para governança local de arquivos estáticos de terceiros.

### Desenvolvimento Assistido por IA
*   **Engenharia de Software Aumentada:** [Google Gemini](https://google.com) - utilizado como copiloto estratégico e assistente sênior de codificação (principalmente Front-end), auxiliando ativamente na modularização arquitetural, regras de negócio e refatorações pontuais.
*   **Testes Unitários e Revisão de Código:** [Claude](https://claude.com/claude-code) (Anthropic, via Claude Code) - utilizado na criação e revisão da suíte de testes da `TicketsController`, incluindo a verificação das regras de acesso por status e a correção de inconsistências encontradas durante os testes. Os commits gerados com apoio do Claude registram a coautoria.

---

## ⚙️ Arquitetura e Padrões de Código Limpo (Clean Code)

*   **Modularização do Pipeline (Program.cs):** Arquivo de inicialização ultra enxuto. Toda a complexidade do sistema foi fracionada em Métodos de Extensão (*Extension Methods*) especializados com responsabilidade única dentro da pasta `/Configuration` (`LocalizationConfiguration`, `DataConfiguration`, `IdentityConfiguration`, `HstsConfiguration`, `DependencyInjectionConfiguration` e `DatabaseSeederConfiguration`).
*   **Centralização de Regras de Domínio:** Eliminação completa de código duplicado (*DRY Principle*) através do encapsulamento de validações operacionais de TI dentro da camada de serviço (`IUsuarioService` / `UsuarioService`), consumida via inversão de controle (`@inject`) tanto em controladores quanto em Views do Razor.
*   **Componentização por Funções Locais:** Telas e menus estruturados através de funções locais do C# (`RenderizarItemMenu`, `RenderizarItemDropdown`, `RenderizarCardKpi`, `RenderizarBotaoAtalho`) em formato literal HTML estritamente tipado, facilitando manutenções em lote na interface de navegação.
*   **Testes Organizados por Ação:** Cada ação da `TicketsController` possui sua própria classe de teste (`IndexTests`, `CreateTests`, `DetailsTests`, `AssumirTests`, `AdicionarComentarioTests`, `MoverParaPendenteTests`, `RetomarAtendimentoTests`, `MoverParaResolvidoTests`, `CancelarTests`, `AlterarPrioridadeTests` e `AlterarItensTests`). Os testes seguem o padrão **Arrange / Act / Assert**, com nomes no formato `Metodo_Cenario_ResultadoEsperado`, e a suíte completa tem 110 testes.

---

## 🧠 Recursos Avançados Mapeados e Implementados

1.  **Motor Multilíngue Dinâmico (i18n):** Suporte total a internacionalização nativa via arquivos de recurso `.resx` para os idiomas **Português (pt-BR)** e **Inglês (en-US)**. Os dicionários utilizam strings puras sem pontos finais, permitindo que chaves sejam reaproveitadas dinamicamente em tabelas, botões e modais.
2.  **Rastreabilidade Contextual de Ativos (ITSM):** Vinculação de múltiplos itens de hardware/software a um único ticket por meio de uma tabela pivot de relacionamento. Técnicos conseguem visualizar o histórico consolidado de incidentes daquele patrimônio específico diretamente na tela de detalhes, alternando os filtros instantaneamente via abas (*Nav-Tabs*) sem requisições extras ao servidor.
3.  **Auditoria Estruturada em JSON:** Registro imutável de logs de alteração e interações anexadas ao chamado armazenados em formato texto serializado, convertendo dinamicamente fusos horários UTC para o horário local do navegador do operador.
4.  **Exclusão Lógica Dinâmica (Soft Delete):** Interface `ISoftDelete` e motor dinâmico via reflexão e expressões Lambda aplicados globalmente no `AppDbContext`. Ao invés de expurgar registros fisicamente, o sistema oculta dados inativos de forma automática e desativa o Cascade Delete em runtime (`DeleteBehavior.Restrict`).

---

## 📝 Observações Cruciais de Governança de Dados

1.  **Truncamento e Fusos Horários (UTC):** Todos os valores de `DateTime` (Abertura, Fechamento, Prazos de Resposta e Resolução) são obrigatoriamente processados e gravados no banco de dados na zona de tempo UTC. A conversão para a cultura local do usuário ocorre estritamente na camada de apresentação (View).
2.  **Segurança de Redirecionamento HTTPS:** Injeção explícita e modular do middleware **HSTS** (HTTP Strict Transport Security) configurada com diretivas de `Preload` e inclusão de subdomínios ativa, blindando a troca de dados em ambientes produtivos.
3.  **Validação Automática Contra Ataques CSRF:** Filtro global `AutoValidateAntiforgeryTokenAttribute` injetado na raiz do pipeline de serviços, forçando a verificação de tokens em todas as requisições que realizam mutação de dados (`POST`/`PUT`/`DELETE`).
4.  **Restrição de Ações por Status:** Cada ação de tramitação verifica quem pode executá-la conforme o status atual do chamado, e a regra é coberta por testes automatizados. Detalhes da matriz estão na seção abaixo.

---

## 👥 Definições de Acesso e Regras de Negócio

O sistema opera sob o modelo RBAC (Controle de Acesso Baseado em Perfis). O menu superior e os botões de atalho da Home se moldam em tempo real dependendo do nível de permissão:

| Perfil (Role) | O que ele faz no sistema? (Regra de Negócio) | O que ele enxerga na Home? (Filtro de Escopo) | Elementos Visíveis de Interface |
| :--- | :--- | :--- | :--- |
| **Usuário Comum** | Abre chamados, acompanha o andamento do seu próprio histórico de incidentes e cancela os seus chamados enquanto estiverem em *Aberto*. | Vê as métricas de contadores de KPI apenas dos chamados abertos por ele mesmo. | Menu de Chamados. Atalhos comuns de abertura e perfil corporativo. |
| **Técnico** | Atende chamados da fila, assume chamados em *Aberto*, altera status de tramitação, estende prazos e resolve incidentes. | Vê as métricas consolidadas de todos os chamados da empresa inteira para monitoramento técnico. | Menu de Chamados, Inventário de Itens/Ativos globais e Painel de Relatórios de SLA. |
| **Admin / Admin Master** | Gerencia as tabelas satélites do catálogo, gerencia usuários, parametriza turnos e feriados. | Vê as métricas completas da empresa inteira corporativa + acesso total às ferramentas de configuração. | Acesso completo a todas as áreas, incluindo o menu suspenso (*Dropdown*) de Administração do Sistema. |

### Matriz de Ações por Status do Chamado

Quem pode agir sobre um chamado depende do status em que ele se encontra:

| Status | Quem pode agir | Ações permitidas |
| :--- | :--- | :--- |
| **Aberto** | O criador do chamado e qualquer técnico | Assumir (técnico), cancelar (criador ou técnico) |
| **Em Atendimento / Pendente** | Somente o técnico atribuído ao chamado | Mover para pendente, retomar atendimento, resolver, alterar prioridade e itens, cancelar |
| **Resolvido / Cancelado** | Ninguém altera o status | Cancelamento e alteração de prioridade e itens são bloqueados |

Técnicos podem visualizar os detalhes de qualquer chamado, enquanto os comentários ficam disponíveis apenas para o criador e para o técnico atribuído. Ao retomar um chamado pausado, o prazo de resolução é estendido pelo tempo em que ele ficou em *Pendente*.
