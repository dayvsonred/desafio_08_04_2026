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
|   +-- sqs-metrics-event.json
|   +-- http-get-metrics-request.json
|   +-- http-get-metrics-response.json
+-- infra/
|   +-- api-gateway/terraform/
|   +-- dynamodb/terraform/
|   +-- sqs/terraform/
+-- microservices/
    +-- receive-complaint/
    +-- classify-complaint/
    +-- process-classified-complaint/
    +-- daily-complaint-metrics/
```

## Nome recomendado da nova lambda

`DailyComplaintMetrics`

## Ordem de execucao das lambdas

1. `ReceiveComplaint.Function` (HTTP POST `/complaints`)
2. `ClassifyComplaint.Function` (SQS classification)
3. `ProcessClassifiedComplaint.Function` (SQS processing)
4. `DailyComplaintMetrics.Function` (SQS metrics + HTTP GET `/metrics?day=yyyyMMdd`)

## Fluxo de negocio

1. `ReceiveComplaint` recebe reclamacao, salva JSON no S3 (`complaint_message_received/yyyyMMdd`), grava `messageReceivedS3Key` no DynamoDB e publica fila de classificacao.
2. `ClassifyComplaint` busca item no DynamoDB, le mensagem no S3, classifica (regras + Bedrock fallback), atualiza DynamoDB e publica fila de processamento.
3. `ProcessClassifiedComplaint` le mensagem no S3, salva JSON final no S3 (`complaint_message_processed/yyyyMMdd`), grava `messageProcessedS3Key` no DynamoDB e finaliza status.
4. As 3 lambdas publicam eventos de metrica na fila `metrics`.
5. `DailyComplaintMetrics` consome essa fila e incrementa contadores diarios na tabela `daily-metrics`.
6. `DailyComplaintMetrics` tambem responde `GET /metrics?day=yyyyMMdd` via API Gateway.

## Tabelas DynamoDB

- `complaints`
- `categories`
- `daily-metrics` (nova)

Exemplos de itens:
- `infra/dynamodb/terraform/dynamodb-items-examples.json`

## Fila SQS

- `classification`
- `processing`
- `metrics` (nova)
- cada uma com DLQ

## Endpoint de metricas

- `GET /metrics?day=20260409`
- retorno:
  - `receivedCount`
  - `classifiedCount`
  - `classificationFailedCount`
  - `processedSuccessCount`
  - `processedErrorCount`

## Build e testes

```bash
dotnet restore ComplaintClassification.sln
dotnet build ComplaintClassification.sln -c Release -m:1

dotnet test microservices/receive-complaint/tests/ReceiveComplaint.UnitTests/ReceiveComplaint.UnitTests.csproj -c Release -m:1
dotnet test microservices/classify-complaint/tests/ClassifyComplaint.UnitTests/ClassifyComplaint.UnitTests.csproj -c Release -m:1
dotnet test microservices/process-classified-complaint/tests/ProcessClassifiedComplaint.UnitTests/ProcessClassifiedComplaint.UnitTests.csproj -c Release -m:1
dotnet test microservices/daily-complaint-metrics/tests/DailyComplaintMetrics.UnitTests/DailyComplaintMetrics.UnitTests.csproj -c Release -m:1
```

## Package das Lambdas + Terraform (copiar e executar)

```powershell
# Base
$Root = "C:\Users\niore\Documents\desafio\desafio_08_04_2026"
$ProjectName = "complaint-classifier-phase1"
$env:AWS_PROFILE = "default"
$env:AWS_REGION = "sa-east-1"

# Build
dotnet restore "$Root\ComplaintClassification.sln"
dotnet build "$Root\ComplaintClassification.sln" -c Release -m:1

# Garantir ferramenta Lambda
dotnet tool update -g Amazon.Lambda.Tools

# Gerar ZIPs
dotnet lambda package --project-location "$Root\microservices\receive-complaint\ReceiveComplaint.Function" --configuration Release --framework net8.0 --output-package "$Root\microservices\receive-complaint\infra\receive-complaint.zip"
dotnet lambda package --project-location "$Root\microservices\classify-complaint\ClassifyComplaint.Function" --configuration Release --framework net8.0 --output-package "$Root\microservices\classify-complaint\infra\classify-complaint.zip"
dotnet lambda package --project-location "$Root\microservices\process-classified-complaint\ProcessClassifiedComplaint.Function" --configuration Release --framework net8.0 --output-package "$Root\microservices\process-classified-complaint\infra\process-classified-complaint.zip"
dotnet lambda package --project-location "$Root\microservices\daily-complaint-metrics\DailyComplaintMetrics.Function" --configuration Release --framework net8.0 --output-package "$Root\microservices\daily-complaint-metrics\infra\daily-complaint-metrics.zip"

# Deploy ReceiveComplaint
terraform -chdir="$Root\microservices\receive-complaint\infra" init
terraform -chdir="$Root\microservices\receive-complaint\infra" apply -auto-approve -var "project_name=$ProjectName" -var "lambda_zip_path=$Root\microservices\receive-complaint\infra\receive-complaint.zip"

# Deploy ClassifyComplaint
terraform -chdir="$Root\microservices\classify-complaint\infra" init
terraform -chdir="$Root\microservices\classify-complaint\infra" apply -auto-approve -var "project_name=$ProjectName" -var "lambda_zip_path=$Root\microservices\classify-complaint\infra\classify-complaint.zip"

# Deploy ProcessClassifiedComplaint
terraform -chdir="$Root\microservices\process-classified-complaint\infra" init
terraform -chdir="$Root\microservices\process-classified-complaint\infra" apply -auto-approve -var "project_name=$ProjectName" -var "lambda_zip_path=$Root\microservices\process-classified-complaint\infra\process-classified-complaint.zip"

# Deploy DailyComplaintMetrics
terraform -chdir="$Root\microservices\daily-complaint-metrics\infra" init
terraform -chdir="$Root\microservices\daily-complaint-metrics\infra" apply -auto-approve -var "project_name=$ProjectName" -var "lambda_zip_path=$Root\microservices\daily-complaint-metrics\infra\daily-complaint-metrics.zip"
```

## Terraform (ordem sugerida)

1. `infra/dynamodb/terraform`
2. `infra/sqs/terraform`
3. package das lambdas (`dotnet lambda package`)
4. `microservices/receive-complaint/infra`
5. `microservices/classify-complaint/infra`
6. `microservices/process-classified-complaint/infra`
7. `microservices/daily-complaint-metrics/infra`
8. `infra/api-gateway/terraform`

Cada pasta Terraform possui `README.md` com comandos PowerShell.
