﻿''' <summary>
''' Provides access to Crowmask's signing key.
''' </summary>
Public Interface ICrowmaskKeyProvider
    ''' <summary>
    ''' Retrieves the public key and renders it in PEM format for use in the ActivityPub actor object.
    ''' </summary>
    ''' <returns>An object that contains the public key in PEM format</returns>
    Function GetPublicKeyAsync() As Task(Of ICrowmaskKey)

    ''' <summary>
    ''' Creates a signature for the given data using the private key.
    ''' </summary>
    ''' <param name="data">The data to sign</param>
    ''' <returns>An RSA SHA-256 signature</returns>
    Function SignRsaSha256Async(data As Byte()) As Task(Of Byte())
End Interface