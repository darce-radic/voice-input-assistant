using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VoiceInputAssistant.Interfaces;

/// <summary>
/// Interface for data storage functionality
/// </summary>
public interface IDataStorageService
{
    /// <summary>
    /// Store a value with the specified key
    /// </summary>
    /// <typeparam name="T">Type of value to store</typeparam>
    /// <param name="key">Storage key</param>
    /// <param name="value">Value to store</param>
    /// <returns>True if successfully stored</returns>
    Task<bool> StoreAsync<T>(string key, T value);

    /// <summary>
    /// Backward-compatible set method
    /// </summary>
    Task<bool> SetAsync<T>(string key, T value);
    
    /// <summary>
    /// Retrieve a value by key
    /// </summary>
    /// <typeparam name="T">Type of value to retrieve</typeparam>
    /// <param name="key">Storage key</param>
    /// <returns>Retrieved value or default</returns>
    Task<T?> RetrieveAsync<T>(string key);

    /// <summary>
    /// Backward-compatible get method with default value
    /// </summary>
    Task<T> GetAsync<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// Check if a key exists in storage
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <returns>True if key exists</returns>
    Task<bool> ExistsAsync(string key);
    
    /// <summary>
    /// Delete a value by key
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <returns>True if successfully deleted</returns>
    Task<bool> DeleteAsync(string key);
    
    /// <summary>
    /// Get all keys in storage
    /// </summary>
    /// <returns>List of all storage keys</returns>
    Task<IEnumerable<string>> GetAllKeysAsync();
    
    /// <summary>
    /// Clear all data from storage
    /// </summary>
    /// <returns>True if successfully cleared</returns>
    Task<bool> ClearAllAsync();
    
    /// <summary>
    /// Get the size of stored data for a key
    /// </summary>
    /// <param name="key">Storage key</param>
    /// <returns>Size in bytes, or -1 if key doesn't exist</returns>
    Task<long> GetSizeAsync(string key);
}