using System;
using Firebase;
using Firebase.Auth;

public static class FirebaseAuthErrorMessages
{
    public static string ForLogin(AggregateException exception)
    {
        string code = GetErrorCode(exception);

        switch (code)
        {
            case "InvalidEmail":
            case "MissingEmail":
                return "Ingresa un correo valido";

            case "UserNotFound":
                return "No existe una cuenta con este correo";

            case "WrongPassword":
            case "InvalidPassword":
                return "Contrasena incorrecta";

            case "InvalidCredential":
                return "Correo no registrado o contrasena incorrecta";

            case "UserDisabled":
                return "Esta cuenta esta deshabilitada";

            case "NetworkRequestFailed":
                return "Revisa tu conexion a internet";

            default:
                return "Correo no registrado o contrasena incorrecta";
        }
    }

    public static string ForRegister(AggregateException exception)
    {
        string code = GetErrorCode(exception);

        switch (code)
        {
            case "EmailAlreadyInUse":
                return "Este correo ya tiene una cuenta registrada";

            case "InvalidEmail":
            case "MissingEmail":
                return "Ingresa un correo valido";

            case "WeakPassword":
                return "La contrasena debe tener al menos 6 caracteres";

            case "MissingPassword":
                return "Ingresa una contrasena";

            case "NetworkRequestFailed":
                return "Revisa tu conexion a internet";

            default:
                return "No se pudo crear la cuenta";
        }
    }

    public static string ForPasswordReset(AggregateException exception)
    {
        string code = GetErrorCode(exception);

        switch (code)
        {
            case "InvalidEmail":
            case "MissingEmail":
                return "Ingresa un correo valido";

            case "UserNotFound":
                return "No existe una cuenta registrada con este correo";

            case "NetworkRequestFailed":
                return "Revisa tu conexion a internet";

            default:
                return "No se pudo enviar el correo de recuperacion";
        }
    }

    private static string GetErrorCode(AggregateException exception)
    {
        FirebaseException firebaseException = FindFirebaseException(exception);

        if (firebaseException == null)
            return string.Empty;

        AuthError errorCode = (AuthError)firebaseException.ErrorCode;
        return errorCode.ToString();
    }

    private static FirebaseException FindFirebaseException(AggregateException exception)
    {
        if (exception == null)
            return null;

        foreach (Exception innerException in exception.Flatten().InnerExceptions)
        {
            FirebaseException firebaseException = innerException as FirebaseException;

            if (firebaseException != null)
                return firebaseException;
        }

        return null;
    }
}