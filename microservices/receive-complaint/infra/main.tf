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
  complaints_table_name     = "${var.project_name}-complaints"
  categories_table_name     = "${var.project_name}-categories"
  classification_queue_name = "${var.project_name}-classification"
  processing_queue_name     = "${var.project_name}-processing"
  metrics_queue_name        = "${var.project_name}-metrics"
}

data "aws_dynamodb_table" "complaints" {
  name = local.complaints_table_name
}

data "aws_dynamodb_table" "categories" {
  name = local.categories_table_name
}

data "aws_sqs_queue" "classification" {
  name = local.classification_queue_name
}

data "aws_sqs_queue" "processing" {
  name = local.processing_queue_name
}

data "aws_sqs_queue" "metrics" {
  name = local.metrics_queue_name
}

resource "aws_iam_role" "lambda_role" {
  name = "${var.project_name}-receive-lambda-role"

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
  name = "${var.project_name}-receive-lambda-policy"
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
          "dynamodb:PutItem"
        ]
        Resource = data.aws_dynamodb_table.complaints.arn
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage"
        ]
        Resource = [
          data.aws_sqs_queue.classification.arn,
          data.aws_sqs_queue.metrics.arn
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject"
        ]
        Resource = "arn:aws:s3:::${var.messages_bucket_name}/complaint_message_received/*"
      }
    ]
  })
}

resource "aws_lambda_function" "receive_complaint" {
  function_name = "${var.project_name}-receive-complaint"
  role          = aws_iam_role.lambda_role.arn
  handler       = "ReceiveComplaint.Function::ReceiveComplaint.Function.Function::FunctionHandler"
  runtime       = "dotnet8"
  timeout       = 30
  memory_size   = 512
  filename      = var.lambda_zip_path

  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      AwsResources__ComplaintsTableName            = data.aws_dynamodb_table.complaints.name
      AwsResources__CategoriesTableName            = data.aws_dynamodb_table.categories.name
      AwsResources__ClassificationQueueUrl         = data.aws_sqs_queue.classification.url
      AwsResources__ProcessingQueueUrl             = data.aws_sqs_queue.processing.url
      AwsResources__MetricsQueueUrl                = data.aws_sqs_queue.metrics.url
      AwsResources__MessagesBucketName             = var.messages_bucket_name
      AwsResources__BedrockModelId                 = var.bedrock_model_id
      Classification__MinimumWinningScore          = "2"
      Classification__MinimumScoreGap              = "1"
      Classification__LowConfidenceThreshold       = "0.75"
      Classification__StrongCategoryRatio          = "0.8"
      Classification__MaxStrongCategoriesBeforeLlm = "2"
    }
  }
}
