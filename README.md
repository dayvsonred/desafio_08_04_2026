# Complaint Classification - Fase 1 (.NET 8 + AWS Serverless)

Implementacao backend de classificacao de reclamacoes com arquitetura serverless na AWS.

## Estrutura (isolamento total por microservico)

Cada microservico possui implementacao propria de `domain`, `application`, `infrastructure`, `function`, `infra` e `tests`.

```text
.
+-- ComplaintClassification.sln
+-- README.md
+-- examples/
�   +-- bedrock-prompt-example.txt
�   +-- http-request.json
�   +-- sqs-classification-event.json
�   +-- sqs-processing-event.json
+-- infra/
�   +-- api-gateway/terraform/
�   +-- dynamodb/terraform/
�   +-- sqs/terraform/
+-- microservices/
�   +-- receive-complaint/
�   �   +-- ReceiveComplaint.Domain/
�   �   +-- ReceiveComplaint.Application/
�   �   +-- ReceiveComplaint.Infrastructure/
�   �   +-- ReceiveComplaint.Function/
�   �   +-- infra/
�   �   +-- tests/ReceiveComplaint.UnitTests/
�   +-- classify-complaint/
�   �   +-- ClassifyComplaint.Domain/
�   �   +-- ClassifyComplaint.Application/
�   �   +-- ClassifyComplaint.Infrastructure/
�   �   +-- ClassifyComplaint.Function/
�   �   +-- infra/
�   �   +-- tests/ClassifyComplaint.UnitTests/
�   +-- process-classified-complaint/
�       +-- ProcessClassifiedComplaint.Domain/
�       +-- ProcessClassifiedComplaint.Application/
�       +-- ProcessClassifiedComplaint.Infrastructure/
�       +-- ProcessClassifiedComplaint.Function/
�       +-- infra/
�       +-- tests/ProcessClassifiedComplaint.UnitTests/
```

## Fluxo implementado

1. API Gateway -> `ReceiveComplaint.Function`
2. Valida payload, gera `complaintId`, persiste status `RECEIVED`, publica em SQS de classificacao
3. `ClassifyComplaint.Function` consome SQS, normaliza texto, classifica por regras e usa Bedrock apenas em fallback
4. Atualiza item para `CLASSIFIED` ou `CLASSIFICATION_FAILED` e publica em SQS de processamento
5. `ProcessClassifiedComplaint.Function` consome fila e marca `PROCESSED` (ou `PROCESSING_FAILED`)

## Regras e fallback

- Normalizacao: minusculas, remocao de acento, remocao de pontuacao, colapso de espacos
- Score por palavra-chave (palavra composta pontua mais)
- Regras primeiro
- Bedrock somente em: sem match, empate, baixa confianca, score baixo, ambiguidade

Limiares configuraveis via variaveis:

- `Classification__MinimumWinningScore`
- `Classification__MinimumScoreGap`
- `Classification__LowConfidenceThreshold`
- `Classification__StrongCategoryRatio`
- `Classification__MaxStrongCategoriesBeforeLlm`

## Observabilidade e robustez

- `complaintId` e `correlationId` em logs
- persistencia de erro no item principal
- retries naturais via SQS
- DLQ provisionada no Terraform SQS
- idempotencia basica por transicoes condicionais de status no DynamoDB

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
3. empacotar lambdas (`dotnet lambda package`)
4. `microservices/receive-complaint/infra`
5. `microservices/classify-complaint/infra`
6. `microservices/process-classified-complaint/infra`
7. `infra/api-gateway/terraform`

## Arquivos de exemplo

- Payload HTTP: `examples/http-request.json`
- Evento SQS classificacao: `examples/sqs-classification-event.json`
- Evento SQS processamento: `examples/sqs-processing-event.json`
- Prompt Bedrock: `examples/bedrock-prompt-example.txt`

## Terraform por pasta

Cada pasta Terraform agora possui um `README.md` com comandos prontos de deploy em PowerShell:

- `infra/dynamodb/terraform/README.md`
- `infra/sqs/terraform/README.md`
- `infra/api-gateway/terraform/README.md`
- `microservices/receive-complaint/infra/README.md`
- `microservices/classify-complaint/infra/README.md`
- `microservices/process-classified-complaint/infra/README.md`



Ordem correta:

dynamodb
sqs
Lambda receive-complaint
Lambda classify-complaint
Lambda process-classified-complaint
api-gateway por último
Motivo:

O API Gateway precisa apontar para a Lambda ReceiveComplaint.
Sem a Lambda criada, você não tem os valores exigidos.
O que são os parâmetros:

INVOKE_ARN (receive_lambda_invoke_arn)
ARN de invocação da Lambda (usado na integração do API Gateway).
FUNCTION_NAME (receive_lambda_name)
Nome da função Lambda (usado no aws_lambda_permission).
De onde pegar:

do output do Terraform de microservices/receive-complaint/infra:
lambda_invoke_arn
lambda_name


Exemplo:

cd microservices\receive-complaint\infra
terraform output lambda_invoke_arn
terraform output lambda_name
