Quero que você implemente uma solução backend da Fase 1 de um sistema de classificação de reclamações, usando arquitetura simples baseada em microserviços serverless na AWS.

## Objetivo
Construir um fluxo de classificação de reclamações com:
- API Gateway
- AWS Lambda em .NET
- DynamoDB
- SQS
- fallback para LLM no Amazon Bedrock somente quando necessário

A solução deve ser simples, clara, organizada e preparada para evolução futura.

sempre gere pasta separadas para cada inte de dominio ou seja uma para cada lambda e uma para o dynamoDB e assim por diante ...
---

## Contexto funcional

O sistema recebe reclamações em um endpoint HTTP com payload como:

{
  "reclamacao": "Estou com problemas para acessar minha conta e o aplicativo está travando muito."
}

Existe uma base de categorias pré-cadastradas no DynamoDB, com nome, palavras-chave e descrição. Exemplo:

{
  "categorias": [
    {
      "nome": "imobiliário",
      "palavrasChave": ["credito imobiliario", "casa", "apartamento"],
      "descricao": "Assuntos relacionados a financiamento ou crédito imobiliário, compra de imóvel, aquisição de casa ou apartamento, contratação de produtos para moradia e dúvidas sobre operações imobiliárias."
    },
    {
      "nome": "seguros",
      "palavrasChave": ["resgate", "capitalizacao", "socorro"],
      "descricao": "Demandas relacionadas a seguros, assistência, capitalização, resgate de valores, acionamento de serviços de suporte, coberturas, apólices e benefícios associados a produtos de proteção financeira."
    },
    {
      "nome": "cobrança",
      "palavrasChave": ["fatura", "cobranca", "valor", "indevido"],
      "descricao": "Problemas relacionados a cobranças, valores lançados em fatura, cobrança indevida, divergência de valores, débito não esperado, contestação de valor cobrado e dúvidas sobre pagamentos ou débitos."
    },
    {
      "nome": "acesso",
      "palavrasChave": ["acessar", "login", "senha"],
      "descricao": "Problemas para entrar na conta, falhas de autenticação, erro de login, senha incorreta, bloqueio de acesso, dificuldade para recuperar credenciais e impossibilidade de acessar serviços autenticados."
    },
    {
      "nome": "aplicativo",
      "palavrasChave": ["app", "aplicativo", "travando", "erro"],
      "descricao": "Falhas técnicas no aplicativo, travamentos, lentidão, erros de funcionamento, telas com problema, instabilidade, dificuldade de uso, falhas após atualização e mau desempenho do app."
    },
    {
      "nome": "fraude",
      "palavrasChave": ["fatura", "nao reconhece divida", "fraude"],
      "descricao": "Situações de possível fraude, transações não reconhecidas, dívida desconhecida, uso indevido da conta, compras suspeitas, movimentações não autorizadas e indícios de ação fraudulenta envolvendo o cliente."
    }
  ]
}

---

## Gateway
    gere ele dentro de uma pasta separada e dentro de sua pasta uma pasta o terraform para subir a lambda

## DynamoDB    
    gere denrtro de uma pasta separada para ele onde deve esta o terraforme de toda estrutura

## SQS 
    gere dentro de uma pasta separada e dentro dela o teraforme dele    

## Lógica de negócio obrigatória

A solução deve seguir exatamente estas etapas:

1. Receber a reclamação via endpoint HTTP.
2. Validar o payload.
3. Gerar um complaintId.
4. Registrar a reclamação recebida no DynamoDB com status inicial `RECEIVED`.
5. Publicar uma mensagem no SQS contendo o `complaintId`.
6. Uma Lambda de classificação deve ser acionada pelo SQS.
7. Essa Lambda deve:
   - buscar a reclamação no DynamoDB pelo `complaintId`
   - normalizar o texto
   - carregar as categorias da tabela DynamoDB
   - calcular score por palavras-chave
   - decidir a categoria vencedora se houver confiança suficiente
   - chamar Bedrock somente se houver empate, baixa confiança, nenhuma correspondência ou ambiguidade
8. O retorno final da classificação deve conter:
   - categoria principal
   - categorias secundárias
   - confiança
   - justificativa curta
   - origem da decisão (`RULES` ou `LLM`)
9. Atualizar a reclamação no DynamoDB com status `CLASSIFIED` ou `CLASSIFICATION_FAILED`.
10. Publicar nova mensagem em outra fila SQS para a etapa seguinte.
11. Uma terceira Lambda deve consumir essa fila e marcar a reclamação como `PROCESSED`.
12. Em caso de falha, registrar erro no item principal e suportar retry/DLQ.

---

## Regras de classificação por palavras-chave

Implemente uma estratégia inicial simples e explicável.

### Normalização obrigatória
- converter texto para minúsculas
- remover acentos
- remover pontuação
- colapsar espaços
- manter texto original e texto normalizado

### Scoring
- cada palavra-chave encontrada soma pontos para a categoria
- palavras compostas podem valer mais que palavras simples
- armazenar também quais palavras-chave deram match

### Decisão
Usar regras primeiro, sem IA, quando:
- existir uma categoria claramente vencedora
- o score da categoria vencedora for suficiente
- a diferença para a segunda colocada for clara

### Chamar LLM no Bedrock apenas quando:
- nenhuma categoria tiver match
- houver empate
- score máximo for baixo
- existirem múltiplas categorias fortes
- o texto for ambíguo

Defina limiares claros no código e documente-os.

---

## Resposta esperada da IA no fallback

Quando a Lambda chamar o Bedrock, a IA deve retornar um JSON estruturado no seguinte formato:

{
  "categoriaPrincipal": "aplicativo",
  "categoriasSecundarias": ["acesso"],
  "confianca": 0.89,
  "justificativa": "O texto descreve travamento do aplicativo e dificuldade de acesso."
}

Crie o prompt do Bedrock de forma objetiva e determinística, pedindo apenas JSON válido.

---

## Arquitetura desejada

Quero uma solução separada em 3 Lambdas quero tbm que seja separadas po pastas e gera dentro da pasta de cada lambda uma pasta chamada infra para conter o terraforme para subir a lambda para AWS 
sendo as principais:

### 1. ReceiveComplaint
Responsabilidades:
- receber requisição do API Gateway
- validar entrada
- gerar ID
- persistir item inicial no DynamoDB
- enviar mensagem para fila de classificação

### 2. ClassifyComplaint
Responsabilidades:
- consumir mensagem da fila de classificação
- buscar reclamação no DynamoDB por chave
- buscar categorias no DynamoDB
- normalizar texto
- classificar via palavras-chave
- acionar Bedrock apenas quando necessário
- atualizar item com resultado
- enviar mensagem para fila de processamento

### 3. ProcessClassifiedComplaint
Responsabilidades:
- consumir mensagem da fila de processamento
- buscar item classificado
- simular tratamento final
- atualizar status para `PROCESSED`

---

## Importante sobre DynamoDB

Não use scan desnecessário para buscar reclamações pendentes.
O fluxo correto deve ser:
- SQS transporta o `complaintId`
- a Lambda busca um item específico no DynamoDB por chave

Use DynamoDB como armazenamento de estado e rastreabilidade.

### Tabelas sugeridas

#### Tabela Complaints
Modelo sugerido:

{
  "PK": "COMPLAINT#123",
  "SK": "METADATA",
  "complaintId": "123",
  "message": "Estou com problemas para acessar minha conta e o aplicativo está travando muito.",
  "normalizedMessage": "estou com problemas para acessar minha conta e o aplicativo esta travando muito",
  "status": "CLASSIFIED",
  "classification": {
    "primaryCategory": "aplicativo",
    "secondaryCategories": ["acesso"],
    "confidence": 0.91,
    "decisionSource": "RULES",
    "scoreBreakdown": [
      {
        "category": "aplicativo",
        "score": 2,
        "matchedKeywords": ["aplicativo", "travando"]
      },
      {
        "category": "acesso",
        "score": 1,
        "matchedKeywords": ["acessar"]
      }
    ],
    "justification": "A reclamação menciona travamento do aplicativo e dificuldade de acesso."
  },
  "createdAt": "2026-04-08T10:00:00Z",
  "updatedAt": "2026-04-08T10:01:00Z",
  "error": null
}

#### Tabela Categories
Pode ser modelada de forma simples, por categoria.

---

## Status obrigatórios

Implemente ao menos estes status:
- RECEIVED
- CLASSIFYING
- CLASSIFIED
- CLASSIFICATION_FAILED
- PROCESSING
- PROCESSED
- PROCESSING_FAILED

---

## Requisitos técnicos

Quero a implementação em .NET, preferencialmente .NET 8, organizada de forma profissional.

### Requisitos de código
- código limpo
- boa separação por responsabilidades
- nomes claros
- tratamento de erro
- logs estruturados
- comments apenas quando realmente agregarem
- classes e interfaces bem definidas
- fácil manutenção

### Estrutura desejada
Organize em camadas ou módulos, por exemplo:
- Functions
- Application
- Domain
- Infrastructure
- Shared

ou estrutura equivalente que faça sentido.

### Esperado
- modelos de request/response
- entidades
- serviços de domínio
- repositórios/interfaces
- adapters para DynamoDB, SQS e Bedrock
- normalizador de texto
- classificador por regras
- orchestrator da decisão entre rules e LLM

---

## Itens que quero no resultado

Gere:

1. Estrutura de pastas do projeto
2. Código das 3 Lambdas separadas pos pasta
3. Modelos e contratos
4. Serviço de normalização
5. Serviço de classificação por regras
6. Serviço de fallback para Bedrock
7. Repositório DynamoDB
8. Cliente SQS
9. Configuração via appsettings/environment variables
10. Exemplo de prompt enviado ao Bedrock
11. Exemplo de eventos SQS
12. README explicando:
   - arquitetura
   - fluxo
   - decisões técnicas
   - como executar
   - como testar localmente
   - como evoluir a solução

---

## Requisitos de observabilidade e robustez

Inclua no código e no README:
- correlationId ou complaintId em logs
- tratamento de exceções
- retry natural do SQS
- recomendação de DLQ
- persistência do erro no item principal
- idempotência básica nas atualizações de status quando possível

---

## Testes

Crie testes unitários para pelo menos:
- normalização do texto
- cálculo de score por categoria
- decisão de categoria vencedora
- cenários que acionam fallback LLM
- cenário de múltiplas categorias

---

## Exemplo funcional esperado

Dado o texto:

"Estou com problemas para acessar minha conta e o aplicativo está travando muito."

A classificação esperada deve ser algo próximo de:
- categoria principal: `aplicativo`
- categorias secundárias: `acesso`

Justificativa:
- encontrou termos relacionados a aplicativo e travamento
- também encontrou termos relacionados a acesso

---

## Restrições importantes

- Mantenha a solução simples, pragmática e de fase inicial
- Não complique com Kubernetes, EventBridge, Step Functions ou múltiplos serviços desnecessários
- Não faça overengineering
- Use Bedrock apenas como fallback
- Priorize clareza arquitetural e explicabilidade

---

## Formato da resposta

Quero que você entregue a resposta em formato de implementação de projeto, com:
- árvore de diretórios
- arquivos principais
- código-fonte
- README
- exemplos de payloads
- exemplos de configuração

Se não conseguir gerar todos os arquivos completos de uma vez, comece pela estrutura principal e pelos arquivos centrais mais importantes.