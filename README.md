# Sistema de Controle - Bairro Brasil

Esse projeto foi desenvolvido como parte da atividade de extensão da faculdade, focando nos pequenos comércios do bairro Brasil Industrial em BH.

A ideia surgiu depois de conversar com alguns comerciantes locais que ainda anotam tudo no papel. O sistema é bem simples mesmo, mas já ajuda a organizar melhor as vendas e ter uma ideia dos números.

**O que tem no sistema:**

- Cadastro de vendas/atendimentos
- Relatórios básicos por período
- Gráficos simples pra visualizar
- Exportar dados em CSV

**Tecnologias usadas:**

- .NET 8 (API)
- SQLite (banco de dados)
- HTML, CSS e JavaScript puro (frontend)
- Swagger pra documentar a API

## Como rodar o projeto

**Você vai precisar ter o .NET 8 instalado**

```bash
cd src/Api
dotnet restore
dotnet run
```

Depois é só acessar:

- Sistema: http://localhost:5187
- Documentação da API: http://localhost:5187/swagger

O banco de dados é criado automaticamente quando você roda pela primeira vez, e já vem com alguns dados de exemplo pra testar.

## Funcionalidades

**API disponível:**

- `GET /api/records` - Lista registros (pode filtrar por data, fonte, categoria)
- `POST /api/records` - Cria novo registro
- `PUT /api/records/{id}` - Atualiza registro
- `DELETE /api/records/{id}` - Remove registro
- `GET /api/records/export.csv` - Exporta dados em CSV
- `GET /api/categories` - Lista categorias
- `POST /api/categories` - Cria categoria
- `GET /health` - Status da API

## Organização dos arquivos

```
bairro-brasil-analytics/
├── src/Api/
│   ├── Program.cs (arquivo principal)
│   ├── BairroBrasilAnalytics.Api.csproj
│   ├── Data/
│   │   └── AppDbContext.cs (configuração do banco)
│   ├── Models/
│   │   ├── Record.cs (modelo dos registros)
│   │   └── Category.cs (modelo das categorias)
│   ├── Dtos/ (objetos para receber dados)
│   └── wwwroot/ (site)
│       ├── index.html
│       ├── style.css
│       └── app.js
├── evidencias/
│   └── exemplo_registros.csv
└── README.md
```

## Sobre o desenvolvimento

Este foi meu primeiro projeto usando .NET 8 e Minimal APIs. Escolhi essa abordagem porque queria algo mais direto e menos verboso que o padrão MVC.

O frontend é bem básico mesmo, usando só HTML/CSS/JS vanilla. Pensei em usar algum framework tipo React, mas achei que ia complicar desnecessariamente pra um projeto de extensão.

O SQLite foi escolha óbvia pra facilitar - não precisa instalar nada, e o arquivo do banco fica junto com o projeto.

## Dados de teste

O sistema já vem com alguns registros de exemplo baseados em negócios reais do bairro:

- Academia (mensalidades, aulas avulsas)
- Lanchonete (vendas de comida)
- Loja de roupas (vendas de produtos)

## Licença

MIT - pode usar à vontade nos seus projetos.
