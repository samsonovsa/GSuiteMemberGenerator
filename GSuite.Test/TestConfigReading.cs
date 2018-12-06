using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GSuite.Libs.Config;

namespace GSuite.Test
{
    [TestClass]
    public class TestConfigReading
    {
        // Arrange
        IConfiguration _config;

        public TestConfigReading()
        {
            // Arrange
            _config = new Configuration();
        }


        [TestMethod]
        public void Read_GSuiteUrl_UrlReturned()
        {
            
            // Act
            string result  = _config.GetURL();

            // Assert
            Assert.AreEqual("https://admin.google.com/", result);
        }

        [TestMethod]
        public void Read_GroupsFileName_FileNameReturned()
        {

            // Act
            string result = _config.GetGroupsFileName();

            // Assert
            Assert.AreEqual("groups.txt", result);
        }

        [TestMethod]
        public void Read_ClientId_ClientIdReturned()
        {

            // Act
            string result = _config.GetClientId();

            // Assert
            Assert.AreEqual("65728268679-7u9e55vghad12uvr2vpvgneg12k1ovop.apps.googleusercontent.com", result);
        }
    }
}
