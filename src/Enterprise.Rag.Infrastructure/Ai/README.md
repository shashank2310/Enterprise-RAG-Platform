# Azure AI adapters

The Semantic Kernel connector is configured with Azure Identity `DefaultAzureCredential`; assign the workload identity only the required Azure OpenAI data-plane role. Deployments remain named configuration values (`AzureOpenAI:ChatDeployment` and `AzureOpenAI:EmbeddingDeployment`) so model choice, capacity, and embedding dimensions are explicit per environment. The grounded generator accepts citations only when their IDs were present in retrieved context. Its confidence is deliberately `0` until a validated/calibrated scorer is connected.

The hybrid retriever always applies tenant and group ACL filters before returning candidates. Provision a matching index schema and semantic configuration before setting `AzureSearch:Endpoint` and `AzureSearch:IndexName`.
