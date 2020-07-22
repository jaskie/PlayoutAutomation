using Microsoft.VisualStudio.TestTools.UnitTesting;
using TAS.Server.CgElementsController.Configurator;
using TAS.Server.CgElementsController.Configurator.Model;

namespace TAS.Server.CgElementsControllerTests.Configurator
{
    [TestClass]
    public class CgElementViewModelTests
    {
        private const string testName = "testName";
        private const string testCommand = "testCommand";        
        private const string testUploadClientImagePath = "testUploadClientPath";
        private const string testUploadServerImagePath = "testUploadServerPath";
        private CgElementViewModel _cgElementViewModel;

        [TestMethod]
        public void EditCgElement()
        {
            var cgElement = new CgElement();
            _cgElementViewModel = new CgElementViewModel(cgElement);

            _cgElementViewModel.Name = testName;
            _cgElementViewModel.Command = testCommand;            
            _cgElementViewModel.UploadClientImagePath = testUploadClientImagePath;            
            _cgElementViewModel.UploadServerImagePath = testUploadServerImagePath;

            _cgElementViewModel.Ok(null);

            Assert.AreEqual(cgElement.Command, testCommand);
            Assert.AreEqual(cgElement.Name, testName);
            Assert.AreEqual(cgElement.UploadClientImagePath, testUploadClientImagePath);
            Assert.AreEqual(cgElement.UploadServerImagePath, testUploadServerImagePath);
        }

        [TestMethod]
        public void IsModified_Modify_True()
        {
            var cgElement = new CgElement();
            _cgElementViewModel = new CgElementViewModel(cgElement);
            Assert.IsFalse(_cgElementViewModel.IsModified);

            _cgElementViewModel.IsModified = false;
            _cgElementViewModel.Name = testName;
            Assert.IsTrue(_cgElementViewModel.IsModified);

            _cgElementViewModel.IsModified = false;
            _cgElementViewModel.Command = testCommand;
            Assert.IsTrue(_cgElementViewModel.IsModified);

            _cgElementViewModel.IsModified = false;
            _cgElementViewModel.UploadClientImagePath = testUploadClientImagePath;
            Assert.IsTrue(_cgElementViewModel.IsModified);

            _cgElementViewModel.IsModified = false;
            _cgElementViewModel.UploadServerImagePath = testUploadServerImagePath;
            Assert.IsTrue(_cgElementViewModel.IsModified);           
        }
    }
}
