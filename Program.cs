using System;
using System.IO;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 1. Configuramos a Windows para que nos pase los errores a nosotros en lugar de cerrar el programa de golpe
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    // Atrapa errores de la interfaz gráfica (formularios, botones)
    static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        LogErrorFatal(e.Exception);
    }

    // Atrapa errores de hilos secundarios o del sistema profundo
    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogErrorFatal(ex);
        }
    }

    // Nuestro motor de guardado de reportes a prueba de balas
    static void LogErrorFatal(Exception ex)
    {
        try
        {
            // Forzamos la ruta absoluta: Siempre será en la carpeta donde está tu ejecutable o proyecto
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = Path.Combine(basePath, "errorlog_fatal.txt");

            string errorMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR FATAL:\n{ex.ToString()}\n{new string('-', 50)}\n";
            
            // File.AppendAllText crea el archivo si no existe, o añade el texto al final si ya existe
            File.AppendAllText(logPath, errorMsg);
            
            // Le mostramos al usuario exactamente DÓNDE se guardó el archivo para que no tenga que adivinar
            MessageBox.Show(
                $"El programa sufrió un error crítico. Se ha guardado el reporte detallado.\n\n" +
                $"Ruta del reporte:\n{logPath}\n\n" +
                $"Detalle rápido: {ex.Message}", 
                "Error fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch
        {
            // Si por algún motivo extremo falla hasta la creación del archivo de texto, mostramos el error directo en pantalla
            MessageBox.Show(
                "Error crítico extremo y fallo al crear el archivo log.\n\n" + ex.Message, 
                "Fallo Catastrófico", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}