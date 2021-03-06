﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using System.Net;
using Hl7.Fhir.WebApi;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore;
using Hl7.DemoFileSystemFhirServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace UnitTestWebApi
{
    [TestClass]
    public class BasicFacade
    {
        #region << Test prepare and cleanup >>
        private IDisposable _fhirServerController;
        public string _baseAddress;

        [TestInitialize]
        public void PrepareTests()
        {
            Type myType = typeof(FhirSTU3Controller);

            // Ensure that we grab an available IP port on the local workstation
            // http://stackoverflow.com/questions/9895129/how-do-i-find-an-available-port-before-bind-the-socket-with-the-endpoint
            string port = "9000";

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                sock.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0)); // Pass 0 here, it means to go looking for a free port
                port = ((IPEndPoint)sock.LocalEndPoint).Port.ToString();
                sock.Close();
            }

            // Now use that randomly located port to start up a local FHIR server
            _baseAddress = "http://localhost:" + port + "/";

            var host = WebHost.CreateDefaultBuilder(null)
                .UseUrls(_baseAddress.TrimEnd('/'))
                .UseContentRoot(Path.GetDirectoryName(this.GetType().Assembly.Location))
                .UseWebRoot(Path.GetDirectoryName(this.GetType().Assembly.Location) + @"\wwwroot")
                .UseEnvironment("Development")
                .UseStartup<Startup>()
                .Build();
            _fhirServerController = host;
            host.RunAsync();
            host.WaitForShutdownAsync();
            System.Diagnostics.Trace.WriteLine($"Listening at {_baseAddress}");
        }

        [TestCleanup]
        public void CleanupTests()
        {
            if (_fhirServerController != null)
                _fhirServerController.Dispose();
        }
        #endregion

        [TestMethod]
        public void GetStaticContent()
        {
            HttpClient c = new HttpClient();
            var result = c.GetAsync(_baseAddress + "content/icon_page.gif").Result;

            System.Diagnostics.Trace.WriteLine(result.ToString());

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode, "Should be status ok");
            // This test occasionally fails when the build doesn't copy over the static files from the referenced Facade project
            Assert.AreEqual("image/gif", result.Content.Headers.ContentType.MediaType, "Should be status ok");
        }

        [TestMethod]
        public void GetCapabilityStatement()
        {
            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);

            var result = clientFhir.CapabilityStatement();
            string xml = new Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(result);
            System.Diagnostics.Trace.WriteLine(xml);

            Assert.IsNotNull(result, "Should be a capability statement returned");
            Assert.IsNotNull(result.FhirVersion, "Should at least report the version of fhir active");
        }

        [TestMethod]
        public void CreatePatient()
        {
            Patient p = new Patient();
            p.Name = new System.Collections.Generic.List<HumanName>();
            p.Name.Add(new HumanName().WithGiven("Grahame").AndFamily("Grieve"));
            p.BirthDate = new DateTime(1970, 3, 1).ToFhirDate(); // yes there are extensions to convert to FHIR format
            p.Active = true;
            p.ManagingOrganization = new ResourceReference("Organization/1", "Demo Org");

            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);
            var result = clientFhir.Create<Patient>(p);

            Assert.IsNotNull(result.Id, "Newly created patient should have an ID");
            Assert.IsNotNull(result.Meta, "Newly created patient should have an Meta created");
            Assert.IsNotNull(result.Meta.LastUpdated, "Newly created patient should have the creation date populated");
            Assert.IsTrue(result.Active.Value, "The patient was created as an active patient");
        }

        [TestMethod]
        public void ReadPatient()
        {
            Patient p = new Patient();
            p.Id = "pat1"; // if you support this format for the IDs (client allocated ID)
            p.Name = new System.Collections.Generic.List<HumanName>();
            p.Name.Add(new HumanName().WithGiven("Grahame").AndFamily("Grieve"));
            p.BirthDate = new DateTime(1970, 3, 1).ToFhirDate(); // yes there are extensions to convert to FHIR format
            p.Active = true;
            p.ManagingOrganization = new ResourceReference("Organization/2", "Other Org");

            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);
            var result = clientFhir.Update<Patient>(p);

            Assert.IsNotNull(result.Id, "Newly created patient should have an ID");
            Assert.IsNotNull(result.Meta, "Newly created patient should have an Meta created");
            Assert.IsNotNull(result.Meta.LastUpdated, "Newly created patient should have the creation date populated");
            Assert.IsTrue(result.Active.Value, "The patient was created as an active patient");

            // read the record to check that it can be loaded
            result = clientFhir.Read<Patient>("Patient/pat1");
            Assert.AreEqual(p.Id, result.Id, "Newly created patient should have an ID");
            Assert.IsNotNull(result.Meta, "Newly created patient should have an Meta created");
            Assert.IsNotNull(result.Meta.LastUpdated, "Newly created patient should have the creation date populated");
            Assert.IsTrue(result.Active.Value, "The patient was created as an active patient");

            try
            {
                var p4 = clientFhir.Read<Patient>("Patient/missing-client-id");
                Assert.Fail("Should have received an exception running this");
            }
            catch (Hl7.Fhir.Rest.FhirOperationException ex)
            {
                // This was the expected outcome
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        [TestMethod]
        public void UpdatePatient()
        {
            Patient p = new Patient();
            p.Id = "pat1"; // if you support this format for the IDs (client allocated ID)
            p.Name = new System.Collections.Generic.List<HumanName>();
            p.Name.Add(new HumanName().WithGiven("Grahame").AndFamily("Grieve"));
            p.BirthDate = new DateTime(1970, 3, 1).ToFhirDate(); // yes there are extensions to convert to FHIR format
            p.Active = true;
            p.ManagingOrganization = new ResourceReference("Organization/2", "Other Org");

            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);
            var result = clientFhir.Update<Patient>(p);

            Assert.IsNotNull(result.Id, "Newly created patient should have an ID");
            Assert.IsNotNull(result.Meta, "Newly created patient should have an Meta created");
            Assert.IsNotNull(result.Meta.LastUpdated, "Newly created patient should have the creation date populated");
            Assert.IsTrue(result.Active.Value, "The patient was created as an active patient");
        }

        [TestMethod]
        public void DeletePatient()
        {
            Patient p = new Patient();
            p.Name = new System.Collections.Generic.List<HumanName>();
            p.Name.Add(new HumanName().WithGiven("Grahame").AndFamily("Grieve"));
            p.BirthDate = new DateTime(1970, 3, 1).ToFhirDate(); // yes there are extensions to convert to FHIR format
            p.Active = true;
            p.ManagingOrganization = new ResourceReference("Organization/1", "Demo Org");

            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);
            var result = clientFhir.Create<Patient>(p);

            Assert.IsNotNull(result.Id, "Newly created patient should have an ID");
            Assert.IsNotNull(result.Meta, "Newly created patient should have an Meta created");
            Assert.IsNotNull(result.Meta.LastUpdated, "Newly created patient should have the creation date populated");
            Assert.IsTrue(result.Active.Value, "The patient was created as an active patient");

            // Now delete the patient we just created
            clientFhir.Delete(result);
        }

        [TestMethod]
        public void WholeSystemHistory()
        {
            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);

            // Create a Patient
            Patient p = new Patient();
            p.Id = "pat1"; // if you support this format for the IDs (client allocated ID)
            p.Name = new System.Collections.Generic.List<HumanName>();
            p.Name.Add(new HumanName().WithGiven("Grahame").AndFamily("Grieve"));
            p.BirthDate = new DateTime(1970, 3, 1).ToFhirDate(); // yes there are extensions to convert to FHIR format
            p.Active = true;
            p.ManagingOrganization = new ResourceReference("Organization/2", "Other Org");
            clientFhir.Update(p);

            // Create an Organization
            Organization org = new Organization();
            org.Id = "2";
            org.Name = "Other Org";
            clientFhir.Update(org);

            // Load the whole history
            var result = clientFhir.WholeSystemHistory();

            Console.WriteLine($"Total All Resources: {result.Total}");
            foreach (var item in result.Entry)
            {
                Console.WriteLine($"{item.FullUrl}");
            }

            Assert.IsNotNull(result.Total, "There should be a total count");
            Assert.IsNotNull(result.Meta, "History Bundle should have an Meta created");
            Assert.IsNotNull(result.Meta.LastUpdated, "History Bundle should have the creation date populated");
            Assert.IsTrue(result.Total.Value > 0, "Should be at least 1 item in the history");
            Assert.AreEqual(result.Total.Value, result.Entry.Count, "Should be matching total and entry counts");

            // Load the Organization type history
            var resultOrgs = clientFhir.TypeHistory<Organization>();

            Console.WriteLine($"Total Org Resources: {resultOrgs.Total}");
            foreach (var item in resultOrgs.Entry)
            {
                Console.WriteLine($"{item.FullUrl}");
            }

            Assert.IsNotNull(resultOrgs.Total, "There should be a total count");
            Assert.IsNotNull(resultOrgs.Meta, "History Bundle should have an Meta created");
            Assert.IsNotNull(resultOrgs.Meta.LastUpdated, "History Bundle should have the creation date populated");
            Assert.IsTrue(resultOrgs.Total.Value > 0, "Should be at least 1 item in the history");
            Assert.AreEqual(resultOrgs.Total.Value, resultOrgs.Entry.Count, "Should be matching total and entry counts");
            Assert.IsTrue(resultOrgs.Total.Value < result.Total.Value, "Should be less orgs than the whole system entry count");
        }

        [TestMethod]
        public void PerformCustomOperationCountResourceTypeInstances()
        {
            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);
            var result = clientFhir.TypeOperation<Patient>("count-em", null, true) as OperationOutcome;
            Assert.IsNotNull(result, "Should be a capability statement returned");
            string xml = new Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(result);
            System.Diagnostics.Trace.WriteLine(xml);
            Assert.AreEqual(1, result.Issue.Count, "Should contain the issue that has the count of the number of resources in there");
            Console.WriteLine($"{result.Issue[0].Details.Text}");
        }

        [TestMethod]
        public void PerformCustomOperationCountAllResourceInstances()
        {
            Hl7.Fhir.Rest.FhirClient clientFhir = new Hl7.Fhir.Rest.FhirClient(_baseAddress, false);
            clientFhir.OnBeforeRequest += ClientFhir_OnBeforeRequest;
            var result = clientFhir.WholeSystemOperation("count-em") as OperationOutcome;
            Assert.IsNotNull(result, "Should be a capability statement returned");
            string xml = new Hl7.Fhir.Serialization.FhirXmlSerializer().SerializeToString(result);
            System.Diagnostics.Trace.WriteLine(xml);
            Assert.AreEqual(2, result.Issue.Count, "Should contain the issue that has the count of the number of resources in there");
            Console.WriteLine($"{result.Issue[0].Details.Text}");
            Assert.IsTrue(result.Issue[1].Details.Text.Contains("x-test: Cleaner"), "Missing the custom header added to the request");
        }

        private void ClientFhir_OnBeforeRequest(object sender, BeforeRequestEventArgs e)
        {
            e.RawRequest.Headers.Add("x-test", "Cleaner");
        }
    }
}
