namespace Coordinator.Services.Abstracts
{
    public interface ITransactionService
    {
        /// <summary>
        /// transaction oluşturur.
        /// </summary>
        /// <returns></returns>
        Task<Guid> CreateTransactionAsync();
        /// <summary>
        /// 1. aşamada servislerin hazır olup olmadığını doğrulamaya yarar.
        /// </summary>
        /// <param name="TransactionId">hangi transactiona karşılık servicelerin hazır olup olmadığını kontrol etmek üzere bir transactionid</param>
        /// <returns></returns>
        Task PrepareServicesAsync(Guid TransactionId);

        /// <summary>
        /// 1. aşamadan 2. aşamaya geçip geçmememizin kararını vermemiz gerekli bunun için tüm servislere karşın node state kontrol edilir,
        /// tüm servisler hazır ise 2. aşamaya geçilir.
        /// </summary>
        /// <param name="TransactionId">hangi transactiona karşılık servicelerin hazır olup olmadığını kontrol etmek üzere bir transactionid</param>
        /// <returns></returns>
        Task<bool> CheckReadyServicesAsync(Guid TransactionId);
        /// <summary>
        /// CheckReadyServices() bunun saysinde tüm servislerin hazır olduğunu öğrendik ve 2. aşamaya geçebiliriz.
        /// commit talimatıyla servisleri çalıştırıyoruz.
        /// </summary>
        /// <param name="TransactionId">hangi transactiona karşılık servicelerin hazır olup olmadığını kontrol etmek üzere bir transactionid</param>
        /// <returns></returns>
        Task CommitAsync(Guid TransactionId);
        /// <summary>
        /// TransactionId bunun ile servislerin işlemlerini tamamlayıp tamamlamadığını kontrol ederiz.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <returns></returns>
        Task<bool> CheckTransactionStateServicesAsync(Guid TransactionId);
        /// <summary>
        /// herhangi bir servis başarılı olamadıysa tüm servislerin işlemlerini geri alırız.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <returns></returns>
        Task RollBackAsync(Guid TransactionId);
    }
}
