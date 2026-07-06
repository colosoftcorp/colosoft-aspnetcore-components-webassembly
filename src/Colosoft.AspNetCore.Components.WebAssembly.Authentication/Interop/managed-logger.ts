import { LogLevel } from './log-level';
import { JavaScriptLoggingOptions } from './java-script-logging-options';

export class ManagedLogger {
  public debug: boolean;
  public trace: boolean;
  public constructor(options: JavaScriptLoggingOptions) {
    this.debug = options.debugEnabled;
    this.trace = options.traceEnabled;
  }

  log(level: LogLevel, message: string): void {
    if (
      (level == LogLevel.Trace && this.trace) ||
      (level == LogLevel.Debug && this.debug)
    ) {
      const levelString = level == LogLevel.Trace ? 'trce' : 'dbug';
      console.debug(
        // Logs in the following format to keep consistency with the way ASP.NET Core logs to the console while avoiding the
        // additional overhead of passing the logger as a JSObjectReference
        // dbug: Colosoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService[0]
        //       <<message>>
        // trce: Colosoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService[0]
        //       <<message>>
        `${levelString}: Colosoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService[0]
      ${message}`,
      );
    }
  }
}
