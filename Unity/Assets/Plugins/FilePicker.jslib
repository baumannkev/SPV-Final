mergeInto(LibraryManager.library, {
    OpenFileDialog: function(gameObjectName, callbackMethod) {
        var input = document.createElement('input');
        input.type = 'file';
        input.accept = '.xml';  // Accept XML files

        input.onchange = function(event) {
            var file = event.target.files[0];
            if (file) {
                var reader = new FileReader();
                reader.onload = function(e) {
                    var fileContent = e.target.result;
                    console.log("File content loaded: " + fileContent);
		    SendMessage("UIManager", "TestMessage", "test success");  
                    SendMessage("UIManager", "OnFileSelected", fileContent);  // Send the file content to Unit
		};
                reader.readAsText(file);
            }
        };

        input.click();  // Trigger the file dialog
    }
});
