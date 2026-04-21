namespace HospitalStatistics.Core.Exceptions
{
    public class RelatedRecordsExistException : Exception
    {
        public RelatedRecordsExistException() : base("Невозможно удалить запись, так как существуют связанные записи.") { }

        public RelatedRecordsExistException(string message) : base(message) { }

        public RelatedRecordsExistException(string message, Exception innerException) : base(message, innerException) { }
    }
}