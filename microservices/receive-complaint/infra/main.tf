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
        Resource = var.complaints_table_arn
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage"
        ]
        Resource = var.classification_queue_arn
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
      AwsResources__ComplaintsTableName    = var.complaints_table_name
      AwsResources__CategoriesTableName    = var.categories_table_name
      AwsResources__ClassificationQueueUrl = var.classification_queue_url
      AwsResources__ProcessingQueueUrl     = var.processing_queue_url
      AwsResources__MessagesBucketName     = var.messages_bucket_name
      AwsResources__BedrockModelId         = var.bedrock_model_id
      Classification__MinimumWinningScore  = "2"
      Classification__MinimumScoreGap      = "1"
      Classification__LowConfidenceThreshold = "0.75"
      Classification__StrongCategoryRatio  = "0.8"
      Classification__MaxStrongCategoriesBeforeLlm = "2"
    }
  }
}