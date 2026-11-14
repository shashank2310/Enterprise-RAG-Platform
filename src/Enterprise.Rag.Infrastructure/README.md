# Infrastructure adapters

This project intentionally contains no cloud SDK implementation yet. Add the provider adapters here, behind the Application ports, after provisioning the resources listed in `docs/architecture.md`. Keep credentials out of configuration files; production workloads should use managed identity and Key Vault references. Update `RagReadiness` only when real dependency checks are implemented.
