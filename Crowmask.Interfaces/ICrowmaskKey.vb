''' <summary>
''' A representation of the public key component of Crowmask's signing key.
''' </summary>
Public Interface ICrowmaskKey
    ''' <summary>
    ''' The public key, in PEM format.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Pem As String
End Interface
