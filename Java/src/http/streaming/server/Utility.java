package http.streaming.server;

import java.text.Format;
import java.text.SimpleDateFormat;
import java.util.Date;

public class Utility {
	
	private static final Format FORMAT = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss");
	
	public static boolean isNullOrEmpty(String text) {
		return text == null || text.length() == 0;
	}
	
	public static String formatDate(long milliseconds) {		// if milliseconds is -1, then it will format current time...
		Date date;

		if (milliseconds == -1) {
			date = new Date();		// current date/time...
		}
		else {
			date = new Date(milliseconds);
		}

		return FORMAT.format(date) + " GMT";
	}

	public static String getFileExtension(String fileName) {
    	return fileName.substring(fileName.lastIndexOf(".") + 1);
    }

}