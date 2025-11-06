using System;
using System.IO;
using System.Reflection;

namespace WincajaLicenseManager.Core
{
    /// <summary>
    /// Clase estática para gestionar el logging de la aplicación.
    /// Escribe logs tanto en consola como en archivo.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogPath;

        /// <summary>
        /// Constructor estático para inicializar la ruta del archivo de log
        /// </summary>
        static Logger()
        {
            // Configurar path del archivo de log en el mismo directorio de la DLL
            string dllDirectory = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location
            );

            LogPath = Path.Combine(
                dllDirectory,
                "WincajaLicenseManager_log.txt"
            );
        }

        /// <summary>
        /// Escribe un mensaje en consola y en el archivo de log
        /// </summary>
        /// <param name="message">Mensaje a registrar</param>
        public static void LogMessage(string message)
        {
            Console.WriteLine(message);
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logEntry);
            }
            catch
            {
                // Si falla el logging a archivo, no interrumpir el proceso
            }
        }

        /// <summary>
        /// Escribe un mensaje de error en consola y en el archivo de log
        /// </summary>
        /// <param name="message">Mensaje de error a registrar</param>
        /// <param name="exception">Excepción opcional a registrar</param>
        public static void LogError(string message, Exception exception = null)
        {
            string errorMessage = exception != null 
                ? $"ERROR: {message} - Exception: {exception.Message}\n{exception.StackTrace}"
                : $"ERROR: {message}";
            
            LogMessage(errorMessage);
        }

        /// <summary>
        /// Escribe un mensaje de información en consola y en el archivo de log
        /// </summary>
        /// <param name="message">Mensaje informativo a registrar</param>
        public static void LogInfo(string message)
        {
            LogMessage($"INFO: {message}");
        }

        /// <summary>
        /// Escribe un mensaje de advertencia en consola y en el archivo de log
        /// </summary>
        /// <param name="message">Mensaje de advertencia a registrar</param>
        public static void LogWarning(string message)
        {
            LogMessage($"WARNING: {message}");
        }

        /// <summary>
        /// Escribe un mensaje de debug en consola y en el archivo de log
        /// </summary>
        /// <param name="message">Mensaje de debug a registrar</param>
        public static void LogDebug(string message)
        {
            LogMessage($"DEBUG: {message}");
        }
    }
}

