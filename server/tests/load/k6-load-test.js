import http from 'k6/http';
import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const wsConnectDuration = new Trend('ws_connect_duration');
const wsMsgDuration = new Trend('ws_msg_duration');
const battlesStarted = new Counter('battles_started');
const battlesCompleted = new Counter('battles_completed');
const desyncRate = new Rate('desync_rate');
const matchmakingTime = new Trend('matchmaking_time');

export const options = {
  stages: [
    { duration: '2m', target: 100 },   // Ramp up to 100 users
    { duration: '5m', target: 1000 },  // Stay at 1000 users
    { duration: '2m', target: 5000 },  // Stress to 5000
    { duration: '5m', target: 5000 },  // Sustain
    { duration: '2m', target: 0 },     // Ramp down
  ],
  thresholds: {
    'ws_connect_duration': ['p(95)<500'],
    'ws_msg_duration': ['p(99)<100'],
    'checks': ['rate>0.99'],
    'battles_completed': ['count>100'],
  },
};

const SERVER_HOST = __ENV.SERVER_HOST || 'localhost';
const WS_PORT = __ENV.WS_PORT || '3001';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || 'test_token';

export default function() {
  const url = `ws://${SERVER_HOST}:${WS_PORT}/battle`;
  const vuId = __VU;
  const token = `${AUTH_TOKEN}_${vuId}`;

  ws.connect(url, {}, function(socket) {
    const connectStart = Date.now();
    
    socket.on('open', () => {
      wsConnectDuration.add(Date.now() - connectStart);
      
      socket.send(JSON.stringify({
        type: 'auth',
        token: token
      }));
    });

    socket.on('message', (msg) => {
      const msgStart = Date.now();
      const data = JSON.parse(msg);
      
      switch (data.type) {
        case 'auth_ok':
          // Join matchmaking
          socket.send(JSON.stringify({
            type: 'matchmake',
            battleType: 'ladder'
          }));
          break;
          
        case 'battle_found':
          battlesStarted.add(1);
          matchmakingTime.add(Date.now() - connectStart);
          
          // Simulate playing cards
          let cardIndex = 0;
          const cards = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];
          
          const playInterval = setInterval(() => {
            if (socket.readyState === ws.OPEN) {
              const cardId = cards[cardIndex % cards.length];
              cardIndex++;
              
              socket.send(JSON.stringify({
                type: 'input',
                input: {
                  type: 'playCard',
                  cardId: cardId,
                  position: { x: 9 + Math.random() * 2, y: 8 + Math.random() * 4 }
                },
                clientTick: Date.now()
              }));
            } else {
              clearInterval(playInterval);
            }
          }, 3000); // Play card every 3 seconds
          
          socket.on('close', () => {
            clearInterval(playInterval);
          });
          break;
          
        case 'game_state':
          wsMsgDuration.add(Date.now() - msgStart);
          
          // Check for desync
          if (data.desync) {
            desyncRate.add(1);
          }
          break;
          
        case 'battle_end':
          battlesCompleted.add(1);
          socket.close();
          break;
          
        case 'error':
          console.error(`VU ${vuId}: Server error: ${data.message}`);
          socket.close();
          break;
      }
    });

    socket.on('close', () => {
      // Connection closed
    });

    socket.on('error', (e) => {
      console.error(`VU ${vuId}: WebSocket error: ${e.error()}`);
    });

    // Timeout after 5 minutes
    socket.setTimeout(() => {
      socket.close();
    }, 300000);
  });

  sleep(1);
}

// Setup function - runs once before test
export function setup() {
  console.log('Starting load test...');
  console.log(`Target: ws://${SERVER_HOST}:${WS_PORT}/battle`);
  return { startTime: Date.now() };
}

// Teardown function - runs once after test
export function teardown(data) {
  const duration = (Date.now() - data.startTime) / 1000;
  console.log(`Load test completed in ${duration}s`);
}