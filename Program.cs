using System;
using System.Windows.Forms;
using System.IO;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Atrapamos cualquier error inesperado de la interfaz o procesos en segundo plano
        Application.ThreadException += (s, e) => LogFatalError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogFatalError(e.ExceptionObject as Exception);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    static void LogFatalError(Exception ex)
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = Path.Combine(baseDir, "errorlog_fatal.txt");
            File.AppendAllText(logPath, $"[{DateTime.Now}] CRASH FATAL:\n{ex}\n\n");
            
            MessageBox.Show("El programa sufrió un error crítico. Se ha guardado el reporte en la carpeta de tu proyecto como 'errorlog_fatal.txt'.\n\nDetalle: " + ex.Message, "Error fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch { }
    }
}