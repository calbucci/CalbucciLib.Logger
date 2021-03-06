# CalbucciLib.Logger

CalbucciLib.Logger provides a simple interface to log app errors, warnings and other events, and route those events to database, files, emails, third party services or more.


## Typical Scenarios and Benefits
- Track unhandled exceptions on ASP.NET, Azure Worker roles, WebJobs, etc.
- Track per-user Error information (useful for support, debug, investigation, etc.)
- Augment each event with your own custom properties
- Track performance thresholds on DB calls


## Patterns
Logger is not meant to use o Trace or Debug alerts. It's meant to log errors, warnings and other issues with production that should be tracked for support, bug fixes, product management, etc.
There are several types of built-in errors:

 - **Info**: Something important that can help investigate issues, but it's not an issue on itself 
 - **Warning**: It's a problem, but the user (or service) wasn't necessarily affected by it
 - **Error**: It's clearly an error and the bug needs to be fixed.
 - **Fatal**: It's catastrophic for the process and the service/process cannot continue in the current state.
 - **Exception**: Similar to Error, but it comes from catching an exception (handled or not)
 - **Perf**: A segment of code took longer than an expected threshold
 - **InvalidCodePath**: A condition that should not exist.
 
## Pattern Examples
 
#### 1) Info
Info should be rarely used or used only temporarily to avoid creating too much noise. One of the biggest problems with typical error logging is too much data, making it hard to find the important and urgent issues with the system. You should use Info when you are trying to investigate a problem and turn it off later.

```csharp
Logger.LogInfo("Sync successful", user.UserId)
```

#### 2) Warning
Warning should be used to indicate a condition the code handled itself, but it wasn't "ideal". For example, an SDK returned a value outside of the range but the code handled that case by using a default (or max).

```csharp
var dateOfBirth = user.GetDateOfBirth();
if(dateOfBirth < oldest)
{
    Logger.LogWarning("Date of birth is older than expected", dateOfBirth);
    dateOfBirth = null;
}
```

#### 3) Error
The difference between an Error and a Warning is that an Error cannot be gracefully handled by code and it it's a bug that must be addressed at some point. 
```csharp
// ...
public void SetStatus(object obj, Status newStatus)
{
    var objWithStatus = obj as IObjectWithStatus;
    if(objWithStatus == null)
    {
        Logger.LogError("Must inherit from IObjectWithStatus", obj, newStatus);
        return;
    }
    // ...
}
```

#### 4) Fatal
Fatal is a type of Error that prevents the system (part or in full) form working properly. Fatal should be used when someone should be paged and take immediate care of the problem. It's urgent and critical!
```csharp
SmtpClient sendGridServer = new SmtpClient("smtp.sendgrid.net", 587);
var sendGridUserName = ConfigurationManager.AppSettings["SendGridUserName"];
var sendGridPassword = EnvironmentSecrets.SendGridPassword;
if(string.IsNullOrEmpty(sendGridUserName) || string.IsNullOrEmpty(sendGridPassword))
{
    Logger.LogFatal("SendGrid account not configured correctly", sendGridUserName);
    throw new Exception("SendGrid account not configured correctly");
}
```


#### 5) Exception
Most exceptions are Errors that should be reported. The LogException makes it easier by taking an Exception class and storing all the information in it.
```csharp
protected void Application_Error(Object sender, EventArgs e)
{
    var exception = Server.GetLastError();
    var httpException = exception as HttpException;
    if(httpException == null || httpException.GetHttpCode() != 404)
    {
        Logger.LogException(exception);
    }
}
```
Or, more commonly used:
```csharp
try
{
    CallbackExecute(param);
}
catch(Exception ex)
{
    Logger.LogException(ex, null, param);
}
```

#### 6) Perf
Perf logging is to indicate a performance issue that just started occuring in the system. It's quite common for us to architect code that works well in the early days, but as changes everywhere occur or the usage grows, perf issues start to show up.
```csharp
foreach(var user in db.Users)
{
    using(var perfLogger = new PerfLogger(TimeSpan.FromSeconds(30), "Weekly email above 30 seconds"))
    {
        SendWeeklyEmail(user);
    }
    // If the operation above takes more than 30 seconds than a call to Logger.LogPerfIssue is made
}
```

#### 7) InvalidCodePath
This kind of logging is useful primarily in future proofing your code, when certain functions were expecting a specific range of values, but calls from the upper layers changed and started passing invalid values.

```csharp
switch(user.Status)
{
    case UserStatus.Active: DoSomething(); break;
    case UserStatus.Delete: return null;
    case UserStatus.Suspended: ShowSuspended(); break;
    default:
        Logger.LogInvalidCodePath("Unknown user status", user.Status);
        break;
}
```



## More Info
### LogEvent class
Each call to LogXXX creates a LogEvent object. This object is a container that can be serialized (to XML, JSON, HTML, Text, etc.) to be sent via email, to a SQL or NoSQL database, or to a third party API (through LogExtensions). It has 6 built-in properties and a Property Bag called "Information" (see LogEvent.cs):
- **Message** (string): The logging message
- **Type** (string): The type of logging (defaults: "Info", "Warning", "Error", "Exception", "Fatal", "PerfIssue" and "InvalidCodePath")
- **StackSignature** (string): A hash of the first 4 stack frames (skipping CalbucciLib.Logger, System.* and Microsoft.*) that uniquely identifies that call stack for easier log grouping.
- **UniqueId** (string): A new guid to uniquely identify this instance.
- **EventDate** (DateTime): The local time of the current event.
- **EventDateUtc** (DateTime): The UTC time of the current event (preferred for DB storage).

#### "Information" Property Bag
The Information property bag is actually a collection of property bags. The first parameter is called "Collection" that defines the bag name, then a Dictionary of name-value pairs.
```csharp
Dictionary<string, Dictionary<string, object>> Information
```
The following categories are added automatically:
- "HttpUser": Properties from HttpContext.Current.User
- "HttpRequest": Properties from HttpContext.Current.Request
- "HttpResponse": Properties from HttpContext.Current.Response
- "HttpSession": Properties from HttpContext.Current.Session (by default this is not include on logging unless you set IncludeSessionObjects to true)
- "CallStack": The call stack frames (you can set IncludeFileNamesInCallStack to true to log the file names). Not this is the call stack of where LogXXX was called, not of the exception itself which will be logged on "Exception" collection.
- "Thread": Properties from the current thread.
- "Process": Properties from the current running process and Assembly.
- "Computer": Properties of the current machine the process is running on.
- "Exception": Properties of the Exception (if any)

You can add, remove or update properties, categories and values.

#### Adding a new Collection
```csharp
logEvent.Add("MyCustomCollection", propertyName, propertyValue);
// or
var collection = logEvent.GetOrCreateCollection("MyCustomCollection");
collection[propertyName] = propertyValue;
```
**WARNING**: You should avoid propertyValue objects that cannot be serialized. At minimum they should have a meaningful implementation of ToString().


### Logging Parameters
All Logging functions support two types of calls:
```csharp
    LogXXX(string format, params object[] args)
    LogXXX(Action<LogEvent> appendData, string format, params object[] args)
```
The "format" string follows the standard string.Format() syntax:
```csharp
    LogError("User {0} invalid state of {1}", user.UserId, user.State);
```
In addition, the parameters don't have to be embedded in the string and they will be simply serialized as "Args" properties.
```csharp
    LogError("User has invalid state", user.UserId, user.State);
```
The former syntax is actually preferred if you are going to use the error message in aggregate to identify volume of errors.
If you have more complex data to be logged you can provide a callback on appendData to be called for you to append additional data information.
```csharp
    LogError(logEvent => {
        logEvent.AddUserData("IncomingCall", phonecall.Number);
        logEvent.Add("CallServer", "ServerId", callserver.ServerId);
    }, "User has invalid state", user.UserId, user.State);
```


### Automatic Email Notification
CalbucciLib.Logger has a built-in email notification extension (You can create your own if this one is not what you are looking for). A lot of times, particularly when starting a new small project, you don't want to store the information or pay for a service, you just want to be notified via email of these errors.
To enable email notification all you need to do is set the SendToEmailAddress property.
```csharp
Logger.Default.SendToEmailAddress = "marcelo@calbucci.com";
```
If you want more control, you have several knobs:
- SendToEmailAddressFatal: A different email address to deal with "Fatal" errors
- EmailFrom: Set the default "from" field
- SubjectLinePrefix: Prefix all messages subject with that string.
- SmtpClient: The default SmtpClient to be used
#### Fully configured email notification
```csharp
Logger.Default.SendToEmailAddress = "devs@calbucci.com";
Logger.Default.SendToEmailAddressFatal = "911@calbucci.com";
Logger.Default.EmailFrom = new MailAddress("Logger", "logger@calbucci.com");
Logger.Default.SubjectLinePrefix = "[ProjectX]";
Logger.Default.SmtpClient = sendGridSmtp;
```


### Configuration
In addition to the built-in email notification described above, a few more parameters are available for you to configure the Logger behavior (all part of Logger.Default or an instance of Logger):
- **MaxHttpFormValueLength**: The maximum numbers of characters to store when serializing the HTTP POST form content (if zero or negative, it means don't serialize the FORM post). For security purposes, all FORM data that has certain substrings ("credit", "pass", "pw", "card", "ssn", etc.) is not serialized.
- **MaxHttpBodyLength**: If the HTTP REQUEST is a POST/PUT and it has a body and the content type of the body is text, it will be stored in LogEvent with the maximum of MaxHttpBodyLength characters. If this value is zero it means to skip serializing the body.
- **IncludeFileNamesInCallStack**: Includes the file names in the call stack serialization. (default: false)
- **IncludeSessionObjects**: Serialize the SessionState (default: false)

### Multiple Loggers
There is a singleton pattern for the default logger, which is the most common use:
```csharp
Logger.Default
```
But multiple instances can be created as well:
```csharp
Logger SyncLogger = new Logger() { ... }
Logger UserLogger = new Logger() { ... }
Logger MixpanelLogger = new Logger() { ... }
```

### Adding Log Extensions
The only thing CalbucciLib.Logger will do by default is to send an email with the logging info. But you might want to do something different with it, like save to the file system, send to a SQL or NoSQL database, call a third party API that aggregates errors, or create your own SMS/Pager mechanic to alert developers/DevOps of problems. Each of these tasks can be easily accomplished via a Log Extension. Each Logger (or the default Logger) can have as many extensions as necessary. The system works this way:
1. A LogXXX function is called (or loggerInstance.XXX)
2. A LogEvent is created and default data is aggregated (HttpContext info, call stack, process info, etc.)
3. Optional data is added to LogEvent via "Action<LogEvent> appendData" parameter
4. Logger.ShouldLogCallback is called (see below)
5. Email is sent (if email is configured)
6. Each registered Log Extension is called with the LogEvent. 

For example, you might decide that you want to save each log file to a directory to be aggregated by a third party service. Here is how you implement your log extension:
```csharp
Logger.Default.AddExtension(logEvent => { 
    string filePath = "d:\log\" + logEvent.EventDate.ToString("yyyyMMddHHmmss") + "-" + logEvent.Type + "-" + logEvent.UniqueId + ".json";
    File.WriteAllText(filePath, logEvent.ToJson(true));
});
```
Or, you might want to get a Twilio SMS message sent to your phone every time there is a fatal error:
```csharp
static DateTime LastFatalSmsSent = DateTime.MinValue;
static int SmsCountLeft = 10;
Logger.Default.AddExtension(logEvent => { 
    if(logEvent.Type != "Fatal")
        return; // only Fatal events
    TimeSpan ts = DateTime.Now - LastFatalSmsSent;
    if(ts.TotalMinutes < 5 || SmsCountLeft == 0)
        return; // don't send more than one text every 5 minutes or more than 10 text messages in total
    Interlocked.Increment(ref SmsCountLeft);
    TwilioClient = new TwilioRestClient(twilioAccountSid, twilioAuthToken);
    TwilioClient.SendSmsMessage(fromPhoneNumber, toPhoneNumber, logEvent.Message);
});
```

### Log Filtering (ShouldLogCallback)
Sometimes it's useful to filter out LogEvents because they are being tracked somewhere else, have already been logged or they are too noise. Here is a simple way to not log the same event more than 50 times per hour.
```csharp
public class LogLimits
{
    static Dictionary<string, int> _LogCounter = new Dictionary<string, int>();
    public static int MaximumEventsPerHour = 50;
    static LogLimits()
    {
        var counterCleanup = new Timer(state =>
        {
            lock (typeof (LogLimits))
            {
                _LogCounter = new Dictionary<string, int>();
            }
        }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    static public bool ShouldLog(LogEvent logEvent)
    {
        if (string.IsNullOrEmpty(logEvent.StackSignature))
             return true; // Can't determine if this event has been logged

        lock (typeof (LogLimits))
        {
            int count = 0;
            _LogCounter.TryGetValue(logEvent.StackSignature, out count);
            if (count >= MaximumEventsPerHour)
                return false;  // Don't log this event anymore
            count++;
            _LogCounter[logEvent.StackSignature] = count;
            return true; // Yes, log it
        }
    }
}

// ...
// Set the ShouldLogCallback
Logger.Default.ShouldLogCallback = LogLimits.ShouldLog;
```

### InternalCrashCallback (a.k.a., when error logging crashes)
If something goes wrong with logging and throws an exception, the code handles it gracefully, but the information on what went wrong is lost. It's a bad idea to try to Log a problem with the Logger using the same mechanic. For example, if the problem is a disk full, trying to write a disk-full log error to disk won't make things better for you. The best solution is to have an alternate log extension just for this cases. Calling a third party API might be a good idea most of the times (unless you are having network issues). Sending an email message is a good idea if you typicall save to disk or DB, and saving to disk is a good idea if you usually use email. 
```csharp
Logger.Default.InternalCrashCallback = exception => { 
    // Do something different with 'exception' but make sure not to call Logger.* or any other function that calls Logger.*
};
```

## Contributors

- Logger was originally created by *Marcelo Calbucci* ([blog.calbucci.com](http://blog.calbucci.com) | [@calbucci](http://twitter.com/calbucci))
	
	
	
