package http.streaming.server;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.FileInputStream;
import java.net.Socket;
import java.util.HashMap;

public class HTTPStreamingServerTaskHandler implements Runnable {

	private Socket socket;

	public HTTPStreamingServerTaskHandler(Socket socket) {
		this.socket = socket;
	}

	private static String readRequest(BufferedInputStream bufferedInputStream) throws Exception {
		byte[] buffer = new byte[Main.BUFFER_SIZE];
		int bytesRead = 0;
		
		String request = "";
		
		// accepting client request...
		while ((bytesRead = bufferedInputStream.read(buffer, 0, buffer.length)) > 0) {
			request += new String(buffer, 0, bytesRead);
			
			// checks if '\r\n' is found...
			if (buffer[bytesRead - 1] == 10 && buffer[bytesRead - 2] == 13) {
				break;
			}
		}

		return request.trim();
	}

	// adds response header to the beginning of the buffer...
	private static int addResponseHeader(byte[] buffer, String responseHeader) {
		byte[] responseHeaderBytes = responseHeader.getBytes();
		int i = 0;
		
		for (i = 0; i < responseHeaderBytes.length; i++) {
			buffer[i] = responseHeaderBytes[i];
		}

		return i;		// returns the offset...
	}

	private static HashMap<String, String> parse(String request) {
		String[][] splits = {
			request.split("\r\n"), null
		};

		HashMap<String, String> responseHeaders = new HashMap<String, String>();

		for (int i = 0; i < splits[0].length; i++) {
			if (i == 0) {
				splits[0][i] = splits[0][i].substring(5);
				splits[0][i] = splits[0][i].substring(0, splits[0][i].length() - 9);

				responseHeaders.put("File-Name", splits[0][i]);
			}
			else {
				splits[1] = splits[0][i].split(":");
				splits[1][1] = splits[1][1].trim();

				if (splits[1][0].equals("Range")) {
					splits[1] = splits[1][1].substring(6).split("-");
					responseHeaders.put("Range-Start", splits[1][0]);
					
					if (splits[1].length == 2) {
						responseHeaders.put("Range-End", splits[1][1]);
					}
				}
				else if (splits[1][0].equals("Host")) {
					responseHeaders.put(splits[1][0], splits[1][1] + ":" + splits[1][2]);
				}
				else {
					responseHeaders.put(splits[1][0], splits[1][1]);
				}
			}
		}

		return responseHeaders;
	}

	@Override
	public void run() {
		byte[] buffer = new byte[Main.BUFFER_SIZE];
		int offset = 0, bytesRead = 0;
		long rangeStart = 0, rangeEnd = 0;

		String fileName, request, responseHeader;
		FileInformation fileInformation = null;
		HashMap<String, String> responseHeaders;
		
		try {
			BufferedInputStream bufferedInputStream = new BufferedInputStream(socket.getInputStream());
			
			if (!Utility.isNullOrEmpty(request = readRequest(bufferedInputStream))) {
				responseHeaders = parse(request);
                fileName = responseHeaders.get("File-Name");
				fileInformation = new FileInformation(fileName);		// exception might occur here if requested media file is not present at the project root directory...
				
				System.out.println("Request\n=======\n\n" + request + "\n");     // just for debugging...
				
				responseHeader = "HTTP/1.1 %s\r\n" +
	                "Date: " + Utility.formatDate(-1) + "\r\n" +
	                "Accept-Ranges: bytes\r\n" +
	                "Host: " + responseHeaders.get("Host") + "\r\n" +
	                "Content-Type: " + fileInformation.getContentType() + "\r\n" +
	                "Last-Modified: " + fileInformation.getLastModified() + "\r\n" +
	                "Pragma: no-cache\r\n" +
	                "Cache-Control: no-cache, no-store, must-revalidate\r\n";
				
                if (responseHeaders.containsKey("Range-Start")) {
                    responseHeader = String.format(responseHeader, "206 Partial Content");

                    if (responseHeaders.containsKey("Range-End") && !Utility.isNullOrEmpty(responseHeaders.get("Range-End"))) {
                        rangeEnd = Long.parseLong(responseHeaders.get("Range-End"));
                    }
                    else {
                        rangeEnd = fileInformation.getContentLength() - 1;
                    }

                    responseHeader += "Content-Range: bytes " + responseHeaders.get("Range-Start") + "-" + rangeEnd + "/" + fileInformation.getContentLength() + "\r\n";
                }
                else {
                    responseHeader = String.format(responseHeader, "200 OK");
                }
                
                responseHeader += "Connection: Keep-Alive\r\n\r\n";
                offset = addResponseHeader(buffer, responseHeader);

                System.out.println("Response\n========\n\n" + responseHeader.trim() + "\n");        // just for debugging...
                
                BufferedOutputStream bufferedOutputStream = new BufferedOutputStream(socket.getOutputStream());
                
                if (responseHeaders.containsKey("Range-Start")) {
                	FileInputStream fileInputStream = new FileInputStream(fileName);
                	
                	rangeStart = Long.parseLong(responseHeaders.get("Range-Start"));

                    if (rangeStart > 0) {
                        fileInputStream.skip(rangeStart);
                    }
                    
                    while ((bytesRead = fileInputStream.read(buffer, offset, buffer.length - offset)) > 0) {
                        try {
                            bufferedOutputStream.write(buffer, 0, offset + bytesRead);
                            bufferedOutputStream.flush();
                            
                            if (offset != 0) {
                            	offset = 0;
                            }
                        }
                        catch (Exception exception) {
                            System.out.println("Exception: " + exception.getMessage() + "\n");

                            break;
                        }
                    }
                    
                    fileInputStream.close();
                }
                else {
                    bufferedOutputStream.write(buffer, 0, offset);
                    bufferedOutputStream.flush();
                }
                
                bufferedOutputStream.close();
			}
		}
		catch (Exception exception) {
			exception.printStackTrace();
		}
	}

}