import AsyncStorage from '@react-native-async-storage/async-storage';
import axios from 'axios';
import { BASE_URL } from '../config';
import { getDb } from '../database/db';
import { syncWithServer } from './syncService';
import { upsertUser } from './userService';

// Backend adresin — config.js'den otomatik alınır

// ─── KAYIT OL ────────────────────────────────────────────────
export const register = async (name, email, password, age) => {
  try {
    console.log('📡 Register isteği gönderiliyor:', BASE_URL);
    const response = await axios.post(`${BASE_URL}/auth/register`, {
      name,
      email,
      password,
      age,
    });
    console.log('✅ Register başarılı:', response.data);
    await AsyncStorage.setItem('token', response.data.token);
    await AsyncStorage.setItem('userId', response.data.userId.toString());
    await AsyncStorage.setItem('userName', response.data.name);
    await AsyncStorage.setItem('userEmail', email);
    // SQLite users tablosuna da yaz — medications için user_id FK eşleşsin
    await upsertUser({ id: response.data.userId, name: response.data.name, email, age: age ?? null }).catch(() => {});
    return response.data;
  } catch (error) {
    console.error('❌ Register hatası:', error.message);
    console.error('❌ Detay:', error.response?.data ?? error.code);
    throw error;
  }
};

// ─── GİRİŞ YAP ───────────────────────────────────────────────
export const login = async (email, password) => {
  const response = await axios.post(`${BASE_URL}/auth/login`, {
    email,
    password,
  });
  await AsyncStorage.setItem('token', response.data.token);
  await AsyncStorage.setItem('userId', response.data.userId.toString());
  await AsyncStorage.setItem('userName', response.data.name);
  await AsyncStorage.setItem('userEmail', email);
  await upsertUser({ id: response.data.userId, name: response.data.name, email, age: null }).catch(() => {});
  syncWithServer().catch(() => {}); // login sonrası bekleyen kayıtları gönder
  return response.data;
};

// ─── ÇIKIŞ YAP ───────────────────────────────────────────────
export const logout = async () => {
  await AsyncStorage.removeItem('token');
  await AsyncStorage.removeItem('userId');
  await AsyncStorage.removeItem('userName');
  await AsyncStorage.removeItem('userEmail');
};

// ─── HESAP SİL ───────────────────────────────────────────────
export const deleteAccount = async () => {
  const token = await AsyncStorage.getItem('token');
  if (token) {
    try {
      await axios.delete(`${BASE_URL}/auth/account`, {
        headers: { Authorization: `Bearer ${token}` },
        timeout: 8000,
      });
    } catch {}
  }
  // Yerel veritabanını temizle
  try {
    const db = await getDb();
    await db.execAsync(`
      DELETE FROM dose_history;
      DELETE FROM reminders;
      DELETE FROM sync_log;
      DELETE FROM medications;
      DELETE FROM users;
    `);
  } catch {}
  // AsyncStorage temizle
  await AsyncStorage.multiRemove(['token', 'userId', 'userName', 'userEmail', 'lastSyncAt', '_notifIds']);
};

// ─── PROFİL GÜNCELLE ────────────────────────────────────────
export const updateProfile = async (name) => {
  const token = await AsyncStorage.getItem('token');
  if (token) {
    try {
      await axios.put(`${BASE_URL}/auth/profile`, { name }, {
        headers: { Authorization: `Bearer ${token}` },
        timeout: 8000,
      });
    } catch {}
  }
  await AsyncStorage.setItem('userName', name);
  const userId = await AsyncStorage.getItem('userId');
  if (userId) {
    const db = await getDb();
    await db.runAsync(`UPDATE users SET name = ? WHERE id = ?;`, [name, parseInt(userId)]);
  }
};

// ─── TOKEN KONTROLÜ ──────────────────────────────────────────
export const getToken = async () => {
  return await AsyncStorage.getItem('token');
};

export const isLoggedIn = async () => {
  const token = await AsyncStorage.getItem('token');
  return token !== null;
};