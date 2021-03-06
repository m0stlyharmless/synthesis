/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.autodesk.bxd;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.List;
import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JOptionPane;
import javax.swing.JTextArea;

/**
 *
 * @author t_sprij
 */
public class Console {

    public static List<Thread> threads = new ArrayList<>();
    public static List<Process> processes = new ArrayList<>();
    public static JTextArea area;
    public static JButton reload;
    public static JFrame frame;
    public static final Object sync = new Object(); // used to sync the console

    public static void start(String file) throws IOException {
        reload.setText("Reload");
        Thread inputStreamThread;
        inputStreamThread = new Thread(() -> {
            try {
                Process process = new ProcessBuilder(file).start();
                processes.add(process);
                InputStream is = process.getInputStream();
                InputStream es = process.getErrorStream();
                InputStreamReader isr = new InputStreamReader(is);
                InputStreamReader esr = new InputStreamReader(es);

                BufferedReader br = new BufferedReader(isr);
                BufferedReader ebr = new BufferedReader(esr);

                Thread errorStreamThread = new Thread(() -> {
                    try {
                        String line;
                        while (true) while ((line = ebr.readLine()) != null) {
                            synchronized (sync) { area.setText(area.getText() + line + "\n"); }
                            Thread.sleep(10); // don't consume all processor speed and give other thread time to grab sync
                        }
                    } catch (IOException | InterruptedException e) {
                        e.printStackTrace();
                        Console.kill();
                    }
                });
                threads.add(errorStreamThread);
                errorStreamThread.start();

                String line;
                while (true) while ((line = br.readLine()) != null) {
                    synchronized (sync) { area.setText(area.getText() + "\n" + line); }
                    Thread.sleep(10); // don't consume all processor speed and give other thread time to grab sync
                }
            } catch (IOException | InterruptedException e) {
                if (e instanceof IOException) {
                    JOptionPane.showMessageDialog(frame, e);
                    new Thread(() -> { Console.kill(); }).start();
                }
            }
        });
        threads.add(inputStreamThread);
        inputStreamThread.start();
    }

    public static void kill() {
        reload.setText("  Start  ");
        threads.stream().forEach((t) -> {
            t.stop();
        });

        processes.stream().forEach((t) -> {
           t.destroyForcibly();
        });
        threads.clear();
        processes.clear();
    }
    
    public static boolean running() {
        return threads.size() > 0;
    }
    
    @Override
    public void finalize() throws Throwable {
       super.finalize();
       kill();
    }
}
