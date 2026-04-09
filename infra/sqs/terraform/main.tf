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

resource "aws_sqs_queue" "classification_dlq" {
  name                      = "${var.project_name}-classification-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "processing_dlq" {
  name                      = "${var.project_name}-processing-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "metrics_dlq" {
  name                      = "${var.project_name}-metrics-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "classification" {
  name                       = "${var.project_name}-classification"
  visibility_timeout_seconds = var.classification_visibility_timeout_seconds
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 10

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.classification_dlq.arn
    maxReceiveCount     = 5
  })
}

resource "aws_sqs_queue" "processing" {
  name                       = "${var.project_name}-processing"
  visibility_timeout_seconds = var.processing_visibility_timeout_seconds
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 10

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.processing_dlq.arn
    maxReceiveCount     = 5
  })
}

resource "aws_sqs_queue" "metrics" {
  name                       = "${var.project_name}-metrics"
  visibility_timeout_seconds = var.metrics_visibility_timeout_seconds
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 10

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.metrics_dlq.arn
    maxReceiveCount     = 5
  })
}
