import { Platform } from 'react-native';

// Android emülatöründe 10.0.2.2 → host makinenin localhost'u
// Fiziksel cihazda veya gerçek IP gerekiyorsa aşağıyı değiştir
const EMULATOR_HOST = '10.0.2.2';
const DEVICE_HOST   = '192.168.1.25'; // Fiziksel cihaz için ipconfig'den bul

// __DEV__ = true ise emülatör/dev ortamı, false ise production
const HOST = Platform.OS === 'android' ? EMULATOR_HOST : 'localhost';

export const BASE_URL = `http://${HOST}:5199/api`;
