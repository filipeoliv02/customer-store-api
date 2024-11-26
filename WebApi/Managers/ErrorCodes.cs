namespace CustomerStoreApi.Managers
{
    /// <summary>
    /// Defines constants that describe error codes.
    /// </summary>
    public static partial class ErrorCodes
    {
        #region Internal Constants

        /// <summary>
        /// The customer already exists.
        /// </summary>
        public const string CustomerAlreadyExists = "Customer Already Exists";

        /// <summary>
        /// The customer does not exist.
        /// </summary>
        public const string CustomerDoesNotExist = "Customer Does Not Exist";

        /// <summary>
        /// The customer does not have an address.
        /// </summary>
        public const string CustomerDoesNotHaveAnAddress = "Customer Does Not Have An Address";

        /// <summary>
        /// Could not get the geolocation of the customer's address.
        /// </summary>
        public const string CouldNotGetGeolocation = "Could Not Get Geolocation from Customer's Address";

        #endregion
    }
}
