package http.streaming.server;

import java.net.InetAddress;
import java.net.ServerSocket;

public class HTTPStreamingServer {
	
	private int port;

    private static ServerSocket serverSocket;
	
	public HTTPStreamingServer(int port) {
		this.port = port;
	}
	
	public void start() throws Exception {
		serverSocket = new ServerSocket(port);
		
		/*
		 * InetAddress.getLocalHost().getHostAddress() might not work properly
		 * depending on the network complexity of the environment...
		 */
		System.out.println("Server running at " + InetAddress.getLocalHost().getHostAddress() + ":" + port + "\n");
		
		while (true) {
			System.out.println("Waiting for clients...\n");
			
			new Thread(new HTTPStreamingServerTaskHandler(serverSocket.accept())).start();
		}
	}
	
}