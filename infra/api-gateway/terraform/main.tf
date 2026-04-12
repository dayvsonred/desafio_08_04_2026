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
  lambda_references = {
    receive_complaint = {
      name       = "complaint-classifier-phase1-receive-complaint"
      arn        = "arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-receive-complaint"
      invoke_arn = "arn:aws:apigateway:sa-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-receive-complaint/invocations"
    }
    classify_complaint = {
      name       = "complaint-classifier-phase1-classify-complaint"
      arn        = "arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-classify-complaint"
      invoke_arn = "arn:aws:apigateway:sa-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-classify-complaint/invocations"
    }
    process_classified_complaint = {
      name       = "complaint-classifier-phase1-process-classified-complaint"
      arn        = "arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-process-classified-complaint"
      invoke_arn = "arn:aws:apigateway:sa-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-process-classified-complaint/invocations"
    }
    daily_complaint_metrics = {
      name       = "complaint-classifier-phase1-daily-complaint-metrics"
      arn        = "arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-daily-complaint-metrics"
      invoke_arn = "arn:aws:apigateway:sa-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:sa-east-1:727646486460:function:complaint-classifier-phase1-daily-complaint-metrics/invocations"
    }
  }
}

resource "aws_apigatewayv2_api" "http_api" {
  name          = "${var.project_name}-api"
  protocol_type = "HTTP"

  cors_configuration {
    allow_origins  = ["*"]
    allow_methods  = ["GET", "POST", "OPTIONS"]
    allow_headers  = ["content-type", "x-correlation-id"]
    expose_headers = ["content-type"]
    max_age        = 300
  }
}

resource "aws_apigatewayv2_integration" "receive_complaint" {
  api_id                 = aws_apigatewayv2_api.http_api.id
  integration_type       = "AWS_PROXY"
  integration_uri        = local.lambda_references.receive_complaint.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_integration" "daily_metrics" {
  api_id                 = aws_apigatewayv2_api.http_api.id
  integration_type       = "AWS_PROXY"
  integration_uri        = local.lambda_references.daily_complaint_metrics.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "post_complaints" {
  api_id    = aws_apigatewayv2_api.http_api.id
  route_key = "POST /complaints"
  target    = "integrations/${aws_apigatewayv2_integration.receive_complaint.id}"
}

resource "aws_apigatewayv2_route" "get_metrics" {
  api_id    = aws_apigatewayv2_api.http_api.id
  route_key = "GET /metrics"
  target    = "integrations/${aws_apigatewayv2_integration.daily_metrics.id}"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.http_api.id
  name        = "$default"
  auto_deploy = true
}

resource "aws_lambda_permission" "allow_api_gateway_receive" {
  statement_id  = "AllowExecutionFromApiGatewayReceive"
  action        = "lambda:InvokeFunction"
  function_name = local.lambda_references.receive_complaint.name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.http_api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "allow_api_gateway_metrics" {
  statement_id  = "AllowExecutionFromApiGatewayMetrics"
  action        = "lambda:InvokeFunction"
  function_name = local.lambda_references.daily_complaint_metrics.name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.http_api.execution_arn}/*/*"
}
