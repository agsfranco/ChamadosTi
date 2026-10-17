# ChamadosTI
**Sistema de gerenciamento de Help Desk para profissionais de TI**
## Em Desenvolvimento
* Sistema em desenvolvimento em Asp.Net Core 10.0 utlizando arquitetura MVC
## 🛠️ Tecnologias e Ferramentas
* **Linguagem / Framework:** [Asp.Net Core 10](https://dotnet.microsoft.com/pt-br/apps/aspnet) - Framework principal.
* **Desenvolvimento Assistido por IA:** [Google Gemini](https://google.com) - utilizado como assistente de codificação, auxiliando na refatoração e geração de trechos de código.

1. Entity Framework
1. Identity
1. Libman
1. FluentAPI
1. Bootstrap
1. Jquery
1. Jquery-validation-unobtrusive

* Observações: 
1. Os valores DateTime estão sendo gravadas no banco em UTC 

## Definições de acesso:

| Perfil (Role) | O que ele faz no sistema? (Regra de Negócio) | O que ele enxerga na Home? (Filtro de Escopo) |
| --- | --- | --- |
| Usuário Comum | Abre chamados e acompanha o andamento do seu próprio histórico. | Vê as métricas apenas dos chamados abertos por ele mesmo. |
| Técnico | Atende chamados da fila, muda status, tramita o SLA e resolve incidentes. | Vê as métricas de todos os chamados da empresa inteira para poder trabalhar. |
| Admin | Gerencia as tabelas satélites (Cadastra setores, prioridades, feriados) e gerencia usuários. | Vê as métricas de todos os chamados da empresa inteira + acesso ao menu de cadastros. |


