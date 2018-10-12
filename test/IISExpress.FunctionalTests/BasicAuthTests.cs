// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class BasicAuthTests : IISFunctionalTestBase
    {
        private readonly PublishedSitesFixture _fixture;

        public BasicAuthTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(DeployerSelector.ServerType)
                .WithTfms(Tfm.NetCoreApp22)
                .WithApplicationTypes(ApplicationType.Portable)
                .WithAllAncmVersions()
                .WithAllHostingModels();

        [ConditionalTheory(Skip = "This test is manual. To run it set ASPNETCORE_MODULE_TEST_USER and ASPNETCORE_MODULE_TEST_PASSWORD environment variables to existing user")]
        [RequiresIIS(IISCapability.BasicAuthentication)]
        [MemberData(nameof(TestVariants))]
        public async Task BasicAuthTest(TestVariant variant)
        {
            var username = Environment.GetEnvironmentVariable("ASPNETCORE_MODULE_TEST_USER");
            var password = Environment.GetEnvironmentVariable("ASPNETCORE_MODULE_TEST_PASSWORD");

            var deploymentParameters = _fixture.GetBaseDeploymentParameters(variant, publish: true);
            deploymentParameters.SetAnonymousAuth(enabled: false);
            deploymentParameters.SetWindowsAuth(enabled: false);
            deploymentParameters.SetBasicAuth(enabled: true);

            // The default in hosting sets windows auth to true.
            var deploymentResult = await DeployAsync(deploymentParameters);
            var request = new HttpRequestMessage(HttpMethod.Get, "/Auth");
            var byteArray = new UTF8Encoding().GetBytes(username + ":" + password);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var response = await deploymentResult.HttpClient.SendAsync(request);

            var responseText = await response.Content.ReadAsStringAsync();

            if (variant.HostingModel == HostingModel.InProcess)
            {
                Assert.StartsWith("Windows", responseText);
                Assert.Contains(username, responseText);
            }
            else
            {
                // We expect out-of-proc not allowing basic auth
                Assert.Equal("Windows:", responseText);
            }
        }
    }
}
