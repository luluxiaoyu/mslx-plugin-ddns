import DDNSSettingsCard from './views/DDNSSettingsCard.vue';
import './style.css';

export const pluginConfig = {
    name: 'DDNSPlugin',
    version: '1.0.0',

    // 注入路由
    routes: [],

    // 注入组件到主系统插槽
    extensions: [
        {
            slot: 'settings-daemon-bottom', // 注入到 设置-系统设置 页面的最下方
            component: DDNSSettingsCard,    // 绑定 DDNS 设置面板
        }
    ]
};