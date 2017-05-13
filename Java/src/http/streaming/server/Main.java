package http.streaming.server;

public class Main {
	
	public static final int BUFFER_SIZE = 8192;
	
	public static void main(String[] args) {
		HTTPStreamingServer httpStreamingServer = new HTTPStreamingServer(8080);    // port = 8080...
		
		try {
			httpStreamingServer.start();
		}
		catch (Exception exception) {
			exception.printStackTrace();
		}
	}
	
}