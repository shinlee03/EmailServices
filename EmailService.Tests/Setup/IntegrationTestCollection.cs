namespace EmailService.Tests.Setup;

[CollectionDefinition("IntegrationTestCollection", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebFactory>
{
    
}