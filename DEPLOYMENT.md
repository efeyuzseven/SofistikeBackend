# AWS test deployment

The test backend runs on AWS ECS/Fargate behind the shared D1 Tech ALB:

- URL: `https://api-sofistike-test.d1-tech.com`
- Health: `https://api-sofistike-test.d1-tech.com/api/v1/system/health`
- ECS service: `sofistike-backend-test`
- ECR repository: `sofistike-backend`
- CloudFormation stack: `sofistike-test`

Every push to `main-prod` runs the backend tests, publishes an immutable image,
runs the EF Core migration/bootstrap task, and then rolls out the ECS service.
The live service is not updated if migration/bootstrap fails.

Infrastructure is declared in `deploy/aws/sofistike-test.yml`. Apply changes with:

```powershell
aws cloudformation deploy `
  --profile d1tech-console `
  --region eu-central-1 `
  --template-file deploy/aws/sofistike-test.yml `
  --stack-name sofistike-test `
  --capabilities CAPABILITY_NAMED_IAM
```

Runtime secrets are encrypted SSM parameters and must never be committed:

- `/sofistike/test/CONNECTION_STRING`
- `/sofistike/test/ADMIN_PASSWORD`

An authorized operator can retrieve the initial admin password with:

```powershell
aws ssm get-parameter `
  --profile d1tech-console `
  --region eu-central-1 `
  --name /sofistike/test/ADMIN_PASSWORD `
  --with-decryption `
  --query Parameter.Value `
  --output text
```

The admin email is `admin@sofistike.com`.
