output "api_endpoint" {
  value = aws_apigatewayv2_api.http_api.api_endpoint
}

output "api_id" {
  value = aws_apigatewayv2_api.http_api.id
}

output "complaints_route" {
  value = "${aws_apigatewayv2_api.http_api.api_endpoint}/complaints"
}

output "metrics_route" {
  value = "${aws_apigatewayv2_api.http_api.api_endpoint}/metrics"
}

output "lambda_references" {
  value = local.lambda_references
}
