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

resource "aws_apigatewayv2_api" "http_api" {
  name          = "${var.project_name}-api"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_integration" "receive_complaint" {
  api_id                 = aws_apigatewayv2_api.http_api.id
  integration_type       = "AWS_PROXY"
  integration_uri        = var.receive_lambda_invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_integration" "daily_metrics" {
  api_id                 = aws_apigatewayv2_api.http_api.id
  integration_type       = "AWS_PROXY"
  integration_uri        = var.metrics_lambda_invoke_arn
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
  function_name = var.receive_lambda_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.http_api.execution_arn}/*/*"
}

resource "aws_lambda_permission" "allow_api_gateway_metrics" {
  statement_id  = "AllowExecutionFromApiGatewayMetrics"
  action        = "lambda:InvokeFunction"
  function_name = var.metrics_lambda_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.http_api.execution_arn}/*/*"
}
