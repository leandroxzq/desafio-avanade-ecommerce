# 🛒 Microserviços de E-commerce

Este projeto implementa uma plataforma de e-commerce para gerenciamento de estoque e vendas, utilizando uma arquitetura de microserviços. O sistema é composto por três microserviços principais e um API Gateway, garantindo comunicação desacoplada, escalabilidade e segurança.

## 🛠️ Tecnologias Utilizadas

![.NET](https://img.shields.io/badge/-.NET%209.0-blueviolet?logo=dotnet)

![C#](https://img.shields.io/badge/C%23-239120?style=flat&logo=unity&logoColor=white)

![Entity Framework Core](https://img.shields.io/badge/-Entity_Framework-8C3D65?logo=dotnet&logoColor=white)

![RabbitMQ](https://img.shields.io/badge/-RabbitMQ-%23FF6600?style=flat&logo=rabbitmq&logoColor=white)

![JWT](https://img.shields.io/badge/JWT-black?style=plastic&logo=JSON%20web%20tokens)

![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)

![Ocelot](https://img.shields.io/badge/Ocelot-512BD4?logo=azuredevops&logoColor=white)

![Swagger](https://img.shields.io/badge/-Swagger-%23Clojure?logo=swagger&logoColor=white)

![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

## 🚀 Como Executar o Projeto

### Pré-requisitos

- **.NET 9 SDK**
- **Docker**

### Passo a Passo

1.  **Clone o repositório:**

    ```
    git clone https://github.com/leandroxzq/desafio-avanade-ecommerce.git
    ```

2.  **Acesse a pasta do projeto:**

    ```
    cd desafio-avanade-ecommerce
    ```

3.  **Construa as imagens do Docker:**

    ```
    docker-compose build --no-cache
    ```

4.  **Inicie os containers:**

    ```
    docker-compose up -d
    ```

5.  **Aplique as migrations no banco de dados (se necessário):**
    ```
    dotnet ef database update
    ```

### Acesso à Aplicação

- **API Gateway**: `http://localhost:5000`
- **Serviço de Estoque**: `http://localhost:5001`
- **Serviço de Autenticação**: `http://localhost:5002`
- **Serviço de Vendas**: `http://localhost:5003`

## 🔑 Autenticação

O sistema utiliza **autenticação JWT** para proteger algumas rotas. Para acessar os endpoints protegidos:

1.  Faça login no **Serviço de Autenticação**.
2.  Copie o token gerado.
3.  Insira o token no cabeçalho **`Authorization`** das suas requisições ou no Swagger, no seguinte formato:
    ```
    Authorization: Bearer {seu_token}
    ```

## 📚 Documentação

Cada microserviço possui sua própria **documentação Swagger** para ajudar a explorar suas funcionalidades. Ao acessar a URL de cada microserviço, você será automaticamente redirecionado para a página do Swagger, onde poderá visualizar e interagir com os endpoints:
