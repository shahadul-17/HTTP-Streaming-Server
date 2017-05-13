package http.streaming.server;

import java.io.BufferedReader;
import java.io.File;
import java.io.InputStreamReader;
import java.util.HashMap;

public class FileInformation {
	
	private long contentLength;
	
    private String fileName, lastModified, contentType;
	
    private static HashMap<String, String> contentTypes = null;
    
    public FileInformation(String fileName) throws Exception {
		this.fileName = fileName;
		
		if (contentTypes == null) {
			loadContentTypes();
        }
		
		retrieveFileInformation();
	}
    
    private void retrieveFileInformation() throws Exception {
    	File file = new File(fileName);
    	lastModified = Utility.formatDate(file.lastModified());
		contentLength = file.length();
    	contentType = contentTypes.get(Utility.getFileExtension(file.getName()));
    }
    
	public long getContentLength() {
		return contentLength;
	}

	public String getFileName() {
		return fileName;
	}

	public String getLastModified() {
		return lastModified;
	}

	public String getContentType() {
		return contentType;
	}

	private static void loadContentTypes() throws Exception {
        String line = "";
        String[] substrings;
        BufferedReader bufferedReader = new BufferedReader(new InputStreamReader(FileInformation.class.getResourceAsStream("/resources/content-types.txt")));
        
        contentTypes = new HashMap<String, String>();
        
        while ((line = bufferedReader.readLine()) != null) {
            substrings = line.split("=");

            contentTypes.put(substrings[0], substrings[1]);
        }
        
    	bufferedReader.close();
    }
	
}