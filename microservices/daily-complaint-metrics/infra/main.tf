terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

locals {
  daily_metrics_table_name = "${var.project_name}-daily-metrics"
  metrics_queue_name       = "${var.project_name}-metrics"
}

data "aws_dynamodb_table" "daily_metrics" {
  name = local.daily_metrics_table_name
}

data "aws_sqs_queue" "metrics" {
  name = local.metrics_queue_name
}

resource "aws_iam_role" "lambda_role" {
  name = "${var.project_name}-daily-metrics-lambda-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy" "lambda_policy" {
  name = "${var.project_name}-daily-metrics-lambda-policy"
  role = aws_iam_role.lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "arn:aws:logs:*:*:*"
      },
      {
        Effect = "Allow"
        Action = [
          "dynamodb:GetItem",
          "dynamodb:UpdateItem",
          "dynamodb:PutItem",
          "dynamodb:Query"
        ]
        Resource = data.aws_dynamodb_table.daily_metrics.arn
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:ChangeMessageVisibility",
          "sqs:GetQueueAttributes"
        ]
        Resource = data.aws_sqs_queue.metrics.arn
      }
    ]
  })
}

resource "aws_lambda_function" "daily_metrics" {
  function_name = "${var.project_name}-daily-complaint-metrics"
  role          = aws_iam_role.lambda_role.arn
  handler       = "DailyComplaintMetrics.Function::DailyComplaintMetrics.Function.Function::FunctionHandler"
  runtime       = "dotnet8"
  timeout       = 30
  memory_size   = 512
  filename      = var.lambda_zip_path

  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      AwsResources__DailyMetricsTableName = data.aws_dynamodb_table.daily_metrics.name
    }
  }
}

resource "aws_lambda_event_source_mapping" "metrics_queue_mapping" {
  event_source_arn        = data.aws_sqs_queue.metrics.arn
  function_name           = aws_lambda_function.daily_metrics.arn
  batch_size              = 10
  function_response_types = ["ReportBatchItemFailures"]
}
