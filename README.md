# Complaint Classification - Fase 1 (.NET 8 + AWS Serverless)

Implementacao backend de classificacao de reclamacoes com arquitetura serverless na AWS, com isolamento total por microservico.

## Estrutura

```text
.
+-- ComplaintClassification.sln
+-- README.md
+-- examples/
|   +-- bedrock-prompt-example.txt
|   +-- http-request.json
|   +-- sqs-classification-event.json
|   +-- sqs-processing-event.json
+-- infra/
|   +-- api-gateway/terraform/
|   +-- dynamodb/terraform/
|   +-- sqs/terraform/
+-- microservices/
    +-- receive-complaint/
    |   +-- ReceiveComplaint.Domain/
    |   +-- ReceiveComplaint.Application/
    |   +-- ReceiveComplaint.Infrastructure/
    |   +-- ReceiveComplaint.Function/
    |   +-- infra/
    |   +-- tests/ReceiveComplaint.UnitTests/
    +-- classify-complaint/
    |   +-- ClassifyComplaint.Domain/
    |   +-- ClassifyComplaint.Application/
    |   +-- ClassifyComplaint.Infrastructure/
    |   +-- ClassifyComplaint.Function/
    |   +-- infra/
    |   +-- tests/ClassifyComplaint.UnitTests/
    +-- process-classified-complaint/
        +-- ProcessClassifiedComplaint.Domain/
        +-- ProcessClassifiedComplaint.Application/
        +-- ProcessClassifiedComplaint.Infrastructure/
        +-- ProcessClassifiedComplaint.Function/
        +-- infra/
        +-- tests/ProcessClassifiedComplaint.UnitTests/
```

## Ordem de execucao das lambdas

1. `ReceiveComplaint.Function` (trigger HTTP via API Gateway)
2. `ClassifyComplaint.Function` (trigger SQS de classificacao)
3. `ProcessClassifiedComplaint.Function` (trigger SQS de processamento)

## Fluxo implementado

1. API Gateway recebe o payload HTTP e chama `ReceiveComplaint.Function`.
2. `ReceiveComplaint` valida a entrada, gera `complaintId` e salva um JSON no S3:
   - bucket: `itau_desafio_2026`
   - prefixo: `complaint_message_received/yyyyMMdd/`
3. `ReceiveComplaint` salva no DynamoDB o item principal com status `RECEIVED` e `messageReceivedS3Key`.
4. `ReceiveComplaint` publica mensagem na fila de classificacao.
5. `ClassifyComplaint` consome a fila, busca o item no DynamoDB, le a mensagem no S3 pelo `messageReceivedS3Key`, aplica regras e usa Bedrock apenas no fallback.
6. `ClassifyComplaint` atualiza o item para `CLASSIFIED`/`CLASSIFICATION_FAILED` e publica na fila de processamento.
7. `ProcessClassifiedComplaint` consome a fila, busca item no DynamoDB, le a mensagem original no S3 e salva um JSON processado em:
   - bucket: `itau_desafio_2026`
   - prefixo: `complaint_message_processed/yyyyMMdd/`
8. `ProcessClassifiedComplaint` salva `messageProcessedS3Key` no DynamoDB e finaliza com status `PROCESSED` (ou `PROCESSING_FAILED`).

## S3 JSONs

### Recebido (`complaint_message_received/...`)

```json
{
  "complaintId": "cmp-123",
  "correlationId": "corr-123",
  "message": "Estou com problemas para acessar minha conta e o aplicativo esta travando muito.",
  "receivedAtUtc": "2026-04-09T17:00:00.0000000Z"
}
```

### Processado (`complaint_message_processed/...`)

```json
{
  "complaintId": "cmp-123",
  "correlationId": "corr-123",
  "messageId": "6c8d6a57-1f2f-4a72-8bd8-7c3e4a0d0d6f",
  "processedAtUtc": "2026-04-09T17:01:00.0000000Z",
  "message": "Estou com problemas para acessar minha conta e o aplicativo esta travando muito.",
  "classification": {
    "primaryCategory": "aplicativo",
    "secondaryCategories": ["acesso"],
    "confidence": 0.91,
    "decisionSource": "RULES",
    "justification": "A reclamacao menciona travamento do aplicativo e dificuldade de acesso.",
    "scoreBreakdown": []
  }
}
```

## Configuracao

Variavel nova em todas as lambdas:

- `AwsResources__MessagesBucketName=itau_desafio_2026`

## Build e testes

```bash
dotnet restore ComplaintClassification.sln
dotnet build ComplaintClassification.sln -c Release -m:1

dotnet test microservices/receive-complaint/tests/ReceiveComplaint.UnitTests/ReceiveComplaint.UnitTests.csproj -c Release -m:1
dotnet test microservices/classify-complaint/tests/ClassifyComplaint.UnitTests/ClassifyComplaint.UnitTests.csproj -c Release -m:1
dotnet test microservices/process-classified-complaint/tests/ProcessClassifiedComplaint.UnitTests/ProcessClassifiedComplaint.UnitTests.csproj -c Release -m:1
```

## Terraform (ordem sugerida)

1. `infra/dynamodb/terraform`
2. `infra/sqs/terraform`
3. package das lambdas (`dotnet lambda package`)
4. `microservices/receive-complaint/infra`
5. `microservices/classify-complaint/infra`
6. `microservices/process-classified-complaint/infra`
7. `infra/api-gateway/terraform`

Cada pasta terraform possui `README.md` com comandos PowerShell de `init/plan/apply`.