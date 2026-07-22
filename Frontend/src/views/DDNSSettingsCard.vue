<template>
  <div class="design-card rounded-2xl glass-card border border-[var(--td-component-border)] shadow-md p-6 text-[var(--td-text-color-primary)]">
    
    <!-- Title Area -->
    <div class="flex items-center gap-3 mb-6 pb-4 border-b border-dashed border-[var(--td-border-level-2-color)]">
      <div class="w-1.5 h-6 bg-[var(--color-primary)] rounded-full shadow-[0_0_8px_var(--color-primary-light)] opacity-90"></div>
      <div class="flex flex-col">
        <h2 class="text-lg font-bold text-[var(--td-text-color-primary)] m-0">动态域名解析 (DDNS)</h2>
        <span class="text-xs text-[var(--td-text-color-secondary)] mt-1 font-medium">将本机 IPv4 / IPv6 地址自动更新到云服务商解析记录</span>
      </div>
      <t-tag v-if="status.isRunning" theme="primary" variant="light" class="ml-auto animate-pulse flex items-center gap-1">
        <template #icon><t-loading size="12px" /></template>
        同步中
      </t-tag>
      <t-tag v-else-if="status.lastSuccessTime" theme="success" variant="light-outline" class="ml-auto">
        就绪
      </t-tag>
      <t-tag v-else theme="warning" variant="light" class="ml-auto">
        待配置
      </t-tag>
    </div>

    <!-- Main Content Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-5 gap-6">
      
      <!-- Left: Configuration Form (3 cols) -->
      <div class="lg:col-span-3 flex flex-col gap-4">
        
        <!-- Global DNS Setting -->
        <h3 class="font-bold text-sm m-0 flex items-center gap-1.5 text-[var(--td-text-color-primary)]">
          <setting-icon /> 服务商配置
        </h3>
        <div class="flex flex-col gap-3 mt-1">
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div class="flex flex-col gap-1.5">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">DNS 服务商</label>
              <t-select v-model="config.provider" placeholder="选择服务商">
                <t-option label="DNSPod (Token)" value="dnspod" />
                <t-option label="腾讯云" value="tencentcloud" />
                <t-option label="阿里云" value="aliyun" />
              </t-select>
            </div>
            <div class="flex flex-col gap-1.5">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">Secret ID / Token ID</label>
              <t-input v-model="config.secretId" placeholder="输入 ID" clearable />
            </div>
            <div class="flex flex-col gap-1.5">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">Secret Key / Token Value</label>
              <t-input 
                v-model="config.secretKey" 
                type="password" 
                :placeholder="isKeyConfigured ? '已配置 (留空不修改)' : '输入 Key'"
                clearable
              />
            </div>
          </div>
          
          <div class="flex flex-col gap-1.5 w-1/3">
            <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">同步间隔 (分钟)</label>
            <t-select v-model="config.syncInterval" placeholder="选择频率">
              <t-option :label="'1 分钟'" :value="1" />
              <t-option :label="'5 分钟'" :value="5" />
              <t-option :label="'10 分钟'" :value="10" />
              <t-option :label="'15 分钟'" :value="15" />
              <t-option :label="'30 分钟'" :value="30" />
            </t-select>
          </div>
        </div>

        <!-- IPv4 Setting -->
        <h3 class="font-bold text-sm mt-2 m-0 flex items-center justify-between text-[var(--td-text-color-primary)]">
          <div class="flex items-center gap-1.5">
            <internet-icon /> IPv4 配置
          </div>
          <t-switch v-model="config.ipv4.enable" />
        </h3>
        <div v-if="config.ipv4.enable" class="flex flex-col gap-3 mt-1 p-3 rounded-lg design-card glass-card border border-[var(--td-component-border)]">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
             <div class="flex flex-col gap-1.5">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">获取方式</label>
              <t-select v-model="config.ipv4.sourceType">
                <t-option label="API 接口" value="api" />
                <t-option label="指定网卡" value="nic" />
                <t-option label="自定义固定 IP" value="custom" />
              </t-select>
            </div>
            
            <div class="flex flex-col gap-1.5" v-if="config.ipv4.sourceType === 'api'">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">API 地址</label>
              <t-input v-model="config.ipv4.apiUrl" placeholder="如 https://api.ipify.org" clearable />
            </div>
            
            <div class="flex flex-col gap-1.5" v-else-if="config.ipv4.sourceType === 'nic'">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">物理网卡</label>
              <t-select v-model="config.ipv4.nicName" placeholder="请选择网卡">
                <t-option v-for="nic in nics" :key="nic" :label="nic" :value="nic" />
              </t-select>
            </div>
            
            <div class="flex flex-col gap-1.5" v-else>
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">固定 IP</label>
              <t-input v-model="config.ipv4.customIP" placeholder="输入静态 IP" clearable />
            </div>
          </div>
          
          <div class="flex flex-col gap-1.5">
            <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">域名列表 (逗号分隔，支持 @.example.com)</label>
            <t-input v-model="config.ipv4.domains" placeholder="如 v4.example.com, @.example.com" clearable />
          </div>
        </div>
        
        <!-- IPv6 Setting -->
        <h3 class="font-bold text-sm mt-2 m-0 flex items-center justify-between text-[var(--td-text-color-primary)]">
          <div class="flex items-center gap-1.5">
            <internet-icon /> IPv6 配置
          </div>
          <t-switch v-model="config.ipv6.enable" />
        </h3>
        <div v-if="config.ipv6.enable" class="flex flex-col gap-3 mt-1 p-3 rounded-lg design-card glass-card border border-[var(--td-component-border)]">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
             <div class="flex flex-col gap-1.5">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">获取方式</label>
              <t-select v-model="config.ipv6.sourceType">
                <t-option label="API 接口" value="api" />
                <t-option label="指定网卡" value="nic" />
                <t-option label="自定义固定 IP" value="custom" />
              </t-select>
            </div>
            
            <div class="flex flex-col gap-1.5" v-if="config.ipv6.sourceType === 'api'">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">API 地址</label>
              <t-input v-model="config.ipv6.apiUrl" placeholder="如 https://api64.ipify.org" clearable />
            </div>
            
            <div class="flex flex-col gap-1.5" v-else-if="config.ipv6.sourceType === 'nic'">
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">物理网卡</label>
              <t-select v-model="config.ipv6.nicName" placeholder="请选择网卡">
                <t-option v-for="nic in nics" :key="nic" :label="nic" :value="nic" />
              </t-select>
            </div>
            
            <div class="flex flex-col gap-1.5" v-else>
              <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">固定 IP</label>
              <t-input v-model="config.ipv6.customIP" placeholder="输入静态 IP" clearable />
            </div>
          </div>
          
          <div class="flex flex-col gap-1.5">
            <label class="text-xs font-semibold text-[var(--td-text-color-secondary)]">域名列表 (逗号分隔，支持 @.example.com)</label>
            <t-input v-model="config.ipv6.domains" placeholder="如 v6.example.com, @.example.com" clearable />
          </div>
        </div>

        <!-- Save Button -->
        <div class="mt-3 flex justify-end">
          <t-button theme="primary" @click="saveSettings" :loading="saving" shape="round" class="w-full md:w-auto px-6">
            保存配置
          </t-button>
        </div>
      </div>

      <!-- Right: Status Panel (2 cols) -->
      <div class="lg:col-span-2 flex flex-col gap-4 border-t lg:border-t-0 lg:border-l border-zinc-200/60 dark:border-zinc-700/60 pt-4 lg:pt-0 lg:pl-6">
        <h3 class="font-bold text-sm m-0 flex items-center gap-1.5 text-[var(--td-text-color-primary)]">
          <dashboard-icon /> 运行状态
        </h3>

        <!-- Status Card -->
        <div class="rounded-xl p-4 design-card glass-card border border-[var(--td-component-border)] flex flex-col gap-3 shadow-inner">
          <div class="flex justify-between items-center mb-2">
            <span class="text-xs text-[var(--td-text-color-secondary)]">DDNS 当前状态</span>
          </div>

          <!-- Current IPs -->
          <div class="flex flex-col gap-2">
            <div v-if="!config.ipv4.enable && !config.ipv6.enable" class="flex items-center justify-center py-2">
              <span class="text-xs text-[var(--td-text-color-placeholder)] font-medium">DDNS 功能尚未启用</span>
            </div>
            <div class="flex flex-col" v-if="config.ipv4.enable">
              <span class="text-xs text-[var(--td-text-color-secondary)] font-semibold">IPv4 地址</span>
              <span class="text-sm font-mono tracking-tight font-bold text-[var(--color-primary)] break-all">
                {{ status.currentIP4 || '--' }}
              </span>
            </div>
            <div class="flex flex-col" v-if="config.ipv6.enable">
              <span class="text-xs text-[var(--td-text-color-secondary)] font-semibold">IPv6 地址</span>
              <span class="text-sm font-mono tracking-tight font-bold text-[var(--color-success)] break-all">
                {{ status.currentIP6 || '--' }}
              </span>
            </div>
          </div>
        </div>

        <!-- Detailed Metas -->
        <div class="flex flex-col gap-2.5 text-xs">
          <div class="flex justify-between">
            <span class="text-[var(--td-text-color-secondary)]">上次同步时间</span>
            <span class="font-medium font-mono">{{ formatDateTime(status.lastSuccessTime) }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-[var(--td-text-color-secondary)]">上次检查时间</span>
            <span class="font-medium font-mono">{{ formatDateTime(status.lastRunTime) }}</span>
          </div>
        </div>

        <!-- Recent Logs Box -->
        <div class="flex flex-col gap-1.5 mt-1">
          <div class="flex justify-between items-center">
            <span class="text-xs text-[var(--td-text-color-secondary)] font-semibold">运行日志</span>
            <button class="text-[10px] text-[var(--td-text-color-placeholder)] hover:text-[var(--color-primary)] transition-colors cursor-pointer" @click="fetchLogs">刷新</button>
          </div>
          <div class="h-32 overflow-y-auto p-2 rounded-lg bg-zinc-900/90 text-zinc-300 font-mono text-[11px] leading-relaxed select-text border border-zinc-700/40">
            <div v-if="logs.length === 0" class="text-zinc-500 text-center py-4">暂无运行日志</div>
            <div 
              v-for="(log, idx) in logs" 
              :key="idx" 
              class="whitespace-pre-wrap break-all py-0.5 border-b border-zinc-800/50 last:border-none" 
              :class="{ 'text-red-400 font-semibold': log.includes('[ERROR]'), 'text-emerald-400': log.includes('变化') || log.includes('更新记录') }"
            >
              {{ log }}
            </div>
          </div>
        </div>

        <!-- Error Warning Box -->
        <div v-if="status.lastErrorMessage" class="p-3 rounded-xl bg-red-50 dark:bg-red-950/20 border border-red-200/50 dark:border-red-900/30 text-xs text-red-600 dark:text-red-400 leading-normal flex flex-col gap-1 shadow-sm">
          <span class="font-bold flex items-center gap-1.5">
            <error-circle-icon class="text-red-500" /> 上次同步异常:
          </span>
          <span class="font-mono break-all">{{ status.lastErrorMessage }}</span>
        </div>

        <!-- Action Button -->
        <div class="mt-auto pt-3">
          <t-button 
            theme="primary" 
            variant="outline"
            @click="triggerRequestNow" 
            :loading="status.isRunning" 
            :disabled="status.isRunning"
            shape="round" 
            class="w-full font-bold"
          >
            {{ status.isRunning ? '同步中...' : '立即同步' }}
          </t-button>
        </div>

      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { MessagePlugin } from 'tdesign-vue-next';
import {
  SettingIcon,
  DashboardIcon,
  ErrorCircleIcon,
  InternetIcon
} from 'tdesign-icons-vue-next';
import request from 'mslx-request';

interface IPConfig {
  enable: boolean;
  sourceType: string;
  customIP: string;
  apiUrl: string;
  nicName: string;
  domains: string;
}

interface DDNSConfig {
  provider: string;
  secretId: string;
  secretKey: string;
  syncInterval: number;
  ipv4: IPConfig;
  ipv6: IPConfig;
}

interface DDNSStatus {
  lastRunTime: string | null;
  lastSuccessTime: string | null;
  lastErrorMessage: string;
  isRunning: boolean;
  currentIP4: string;
  currentIP6: string;
}

const BASE_URL = '/api/plugins/mslx-plugin-ddns/ddns';

const config = ref<DDNSConfig>({
  provider: 'dnspod',
  secretId: '',
  secretKey: '',
  syncInterval: 5,
  ipv4: { enable: false, sourceType: 'api', customIP: '', apiUrl: 'https://api.ipify.org', nicName: '', domains: '' },
  ipv6: { enable: false, sourceType: 'api', customIP: '', apiUrl: 'https://api6.ipify.org', nicName: '', domains: '' }
});

const status = ref<DDNSStatus>({
  lastRunTime: null,
  lastSuccessTime: null,
  lastErrorMessage: '',
  isRunning: false,
  currentIP4: '',
  currentIP6: ''
});

const logs = ref<string[]>([]);
const nics = ref<string[]>([]);
const saving = ref(false);
let timerId: number | null = null;
const isKeyConfigured = ref(false);

const fetchConfig = async () => {
  try {
    const res = await request.get({ url: `${BASE_URL}/config` });
    if (res) {
      config.value = {
        ...config.value,
        ...res,
        ipv4: res.ipv4 || res.iPv4 || res.IPv4 || config.value.ipv4,
        ipv6: res.ipv6 || res.iPv6 || res.IPv6 || config.value.ipv6
      };
      
      if (res.secretKey === '******') {
        isKeyConfigured.value = true;
        config.value.secretKey = '';
      } else {
        isKeyConfigured.value = false;
      }
    }
  } catch (err: any) {
    console.error('[DDNS] 获取配置失败', err);
  }
};

const fetchNics = async () => {
  try {
    const res = await request.get({ url: `${BASE_URL}/nics` });
    if (res) {
      nics.value = res;
    }
  } catch (err: any) {
    console.error('[DDNS] 获取网卡失败', err);
  }
};

const fetchStatus = async () => {
  try {
    const res = await request.get({ url: `${BASE_URL}/status` });
    if (res) {
      status.value = res;
    }
  } catch (err: any) {
    console.error('[DDNS] 获取状态失败', err);
  }
};

const fetchLogs = async () => {
  try {
    const res = await request.get({ url: `${BASE_URL}/logs` });
    if (res && Array.isArray(res)) {
      logs.value = res;
    }
  } catch (err: any) {
    console.error('[DDNS] 获取日志失败', err);
  }
};

const saveSettings = async () => {
  try {
    saving.value = true;
    await request.post({
      url: `${BASE_URL}/save-config`,
      data: config.value
    });
    MessagePlugin.success('保存成功');
    fetchConfig();
    fetchLogs();
  } catch (err: any) {
    MessagePlugin.error(err.message || '保存配置失败');
  } finally {
    saving.value = false;
  }
};

const triggerRequestNow = async () => {
  try {
    await request.post({ url: `${BASE_URL}/request-now` });
    MessagePlugin.success('同步任务已启动！');
    fetchStatus();
    fetchLogs();
    startPolling(3000);
  } catch (err: any) {
    MessagePlugin.error(err.message || '触发任务异常');
  }
};

const formatDateTime = (dateStr: string | null) => {
  if (!dateStr) return '暂无数据';
  const d = new Date(dateStr);
  if (isNaN(d.getTime())) return dateStr;
  
  return d.toLocaleString('zh-CN', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
    hour12: false
  });
};

const startPolling = (interval: number) => {
  stopPolling();
  timerId = window.setInterval(async () => {
    await fetchStatus();
    await fetchLogs();
    if (!status.value.isRunning && interval === 3000) {
      startPolling(10000);
    } else if (status.value.isRunning && interval === 10000) {
      startPolling(3000);
    }
  }, interval);
};

const stopPolling = () => {
  if (timerId !== null) {
    window.clearInterval(timerId);
    timerId = null;
  }
};

onMounted(async () => {
  await fetchConfig();
  await fetchNics();
  await fetchStatus();
  await fetchLogs();
  startPolling(10000);
});

onUnmounted(() => {
  stopPolling();
});
</script>

<style scoped>
@unocss;
.bg-primary-light { background-color: color-mix(in srgb, var(--td-brand-color) 10%, transparent); }
</style>
